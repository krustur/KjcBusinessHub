using System.Text.RegularExpressions;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Application.Services;

public partial class TransactionImportService(
    ITransactionRepository repository,
    ILogger<TransactionImportService> logger)
{
    public const string TransactionsFileName = "Consulting-Transactions.txt";

    [GeneratedRegex(@"^\s*\[[\ \-X]\]\s*", RegexOptions.Compiled)]
    private static partial Regex CheckboxPrefix();

    [GeneratedRegex(@"#.*$", RegexOptions.Compiled)]
    private static partial Regex CommentSuffix();

    /// <summary>
    /// Parses a decimal in the Swedish number format used in the file:
    /// optional minus, space as thousands separator, comma as decimal separator, 2 decimals.
    /// Examples: "-499,00"  "99 897,12"
    /// </summary>
    private static decimal ParseSwedishDecimal(string value)
    {
        // Remove thousands separator (space), replace decimal comma with dot
        var normalised = value.Trim().Replace(" ", "").Replace(",", ".");
        return decimal.Parse(normalised, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task ImportAsync(string sourceDocumentFolder, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(sourceDocumentFolder, TransactionsFileName);
        if (!File.Exists(filePath))
        {
            logger.LogWarning("Transactions file not found at {FilePath}.", filePath);
            return;
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        await ProcessLinesAsync(lines, cancellationToken);
    }

    public async Task ProcessLinesAsync(string[] lines, CancellationToken cancellationToken = default)
    {
        var parsedTransactions = new List<ParsedLine>();

        for (int lineNumber = 1; lineNumber <= lines.Length; lineNumber++)
        {
            var rawLine = lines[lineNumber - 1];

            // Strip comment
            var line = CommentSuffix().Replace(rawLine, "").Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                logger.LogDebug("Line {LineNumber}: empty line, skipping.", lineNumber);
                continue;
            }

            // Skip checkbox prefix
            line = CheckboxPrefix().Replace(line, "");

            // Try to parse
            var parsed = TryParseLine(line, lineNumber);
            if (parsed is null) continue;

            parsedTransactions.Add(parsed);
        }

        // Load all existing transactions once
        var allExisting = await repository.GetAllAsync(cancellationToken);

        // Build a set of file-transaction keys for detecting deletions
        var fileKeys = new HashSet<string>(parsedTransactions.Select(p => MakeKey(p.AccountingDate, p.TransactionDate, p.Description, p.Amount, p.Balance)));

        // Process file transactions
        foreach (var parsed in parsedTransactions)
        {
            var existing = allExisting.FirstOrDefault(t =>
                t.AccountingDate == parsed.AccountingDate &&
                t.TransactionDate == parsed.TransactionDate &&
                t.Description == parsed.Description &&
                t.Amount == parsed.Amount &&
                t.Balance == parsed.Balance);

            if (existing is not null)
            {
                if (existing.Status is TransactionStatus.RemovedFromFile or TransactionStatus.Removed)
                {
                    existing.Status = TransactionStatus.Active;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await repository.UpdateAsync(existing, cancellationToken);
                    logger.LogWarning("Line {LineNumber}: transaction re-activated: {Description}.", parsed.LineNumber, parsed.Description);
                }
                else
                {
                    logger.LogDebug("Line {LineNumber}: transaction already exists, skipping: {Description}.", parsed.LineNumber, parsed.Description);
                }
            }
            else
            {
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountingDate = parsed.AccountingDate,
                    TransactionDate = parsed.TransactionDate,
                    Description = parsed.Description,
                    Amount = parsed.Amount,
                    Balance = parsed.Balance,
                    Status = TransactionStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                await repository.AddAsync(transaction, cancellationToken);
                logger.LogInformation("Line {LineNumber}: added transaction: {Description}.", parsed.LineNumber, parsed.Description);
            }
        }

        // Mark removed transactions
        foreach (var existing in allExisting)
        {
            if (existing.Status == TransactionStatus.Active)
            {
                var key = MakeKey(existing.AccountingDate, existing.TransactionDate, existing.Description, existing.Amount, existing.Balance);
                if (!fileKeys.Contains(key))
                {
                    existing.Status = TransactionStatus.RemovedFromFile;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await repository.UpdateAsync(existing, cancellationToken);
                    logger.LogWarning("Transaction no longer in file, marked RemovedFromFile: {Description}.", existing.Description);
                }
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    private static string MakeKey(DateOnly accountingDate, DateOnly transactionDate, string description, decimal amount, decimal balance)
        => $"{accountingDate}|{transactionDate}|{description}|{amount:F2}|{balance:F2}";

    private ParsedLine? TryParseLine(string line, int lineNumber)
    {
        try
        {
            var remaining = line.AsSpan();

            // AccountingDate: yyyy-mm-dd
            if (remaining.Length < 10 || remaining[4] != '-' || remaining[7] != '-')
            {
                logger.LogError("Line {LineNumber}: cannot parse AccountingDate from '{Line}'.", lineNumber, line);
                return null;
            }
            var accountingDate = DateOnly.ParseExact(remaining[..10].ToString(), "yyyy-MM-dd");
            remaining = remaining[10..].TrimStart();

            // TransactionDate: yyyy-mm-dd
            if (remaining.Length < 10 || remaining[4] != '-' || remaining[7] != '-')
            {
                logger.LogError("Line {LineNumber}: cannot parse TransactionDate from '{Line}'.", lineNumber, line);
                return null;
            }
            var transactionDate = DateOnly.ParseExact(remaining[..10].ToString(), "yyyy-MM-dd");
            remaining = remaining[10..].TrimStart();

            // Description: up to (but not including) a tab character
            var tabIndex = remaining.IndexOf('\t');
            if (tabIndex < 0)
            {
                logger.LogError("Line {LineNumber}: missing tab separator after Description in '{Line}'.", lineNumber, line);
                return null;
            }
            var description = remaining[..tabIndex].ToString().TrimEnd();
            remaining = remaining[(tabIndex + 1)..].TrimStart();

            // Amount and Balance: two space-separated Swedish decimals
            // Amount ends at the space before Balance; Balance goes to the end
            // Since amount may contain spaces (thousands sep), we need to split from the right:
            // The last two tokens separated by a single space (no second space after comma) are Balance.
            // Strategy: split on tab, but here we already trimmed tab. Use rightmost token by tab if present,
            // else split: the last "word group" is balance. Amount may have a space inside (e.g. "99 897,12").
            // Tokens separated by whitespace from the right: last token = balance, second-to-last may be part of amount.
            var parts = remaining.ToString().Split('\t', 2);
            string amountStr;
            string balanceStr;
            if (parts.Length == 2)
            {
                amountStr = parts[0].Trim();
                balanceStr = parts[1].Trim();
            }
            else
            {
                // No second tab: split on whitespace carefully.
                // Pattern: amount ends at last comma+2digits, balance is after the next whitespace block.
                // Simpler: find the last occurrence of a comma, 2 digits, then a space.
                var str = remaining.ToString().Trim();
                // Find boundary: look for pattern "d,dd " or "dd,dd " – the Amount ends at the comma+2 digits
                // then whitespace separates Balance.
                var amountBalanceRegex = new Regex(@"^([\d\- ,]+,\d{2})\s+([\-\d ,]+,\d{2})$");
                var m = amountBalanceRegex.Match(str);
                if (!m.Success)
                {
                    logger.LogError("Line {LineNumber}: cannot parse Amount/Balance from '{Remaining}'.", lineNumber, str);
                    return null;
                }
                amountStr = m.Groups[1].Value;
                balanceStr = m.Groups[2].Value;
            }

            var amount = ParseSwedishDecimal(amountStr);
            var balance = ParseSwedishDecimal(balanceStr);

            return new ParsedLine(lineNumber, accountingDate, transactionDate, description, amount, balance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Line {LineNumber}: failed to parse '{Line}'.", lineNumber, line);
            return null;
        }
    }

    private sealed record ParsedLine(
        int LineNumber,
        DateOnly AccountingDate,
        DateOnly TransactionDate,
        string Description,
        decimal Amount,
        decimal Balance);
}
