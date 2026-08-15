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

    /// <summary>
    /// Matches an optional checkbox prefix containing zero or one character of any kind.
    /// Examples: "[ ]", "[X]", "[-]", "[1]", "[]"
    /// </summary>
    [GeneratedRegex(@"^\s*\[.?\]\s*", RegexOptions.Compiled)]
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

            // Strip optional checkbox prefix
            line = CheckboxPrefix().Replace(line, "");

            // Try to parse
            var parsed = TryParseLine(line, lineNumber);
            if (parsed is null) { continue; }

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
        // All fields are separated by one or more tabs — collapse runs of tabs first
        while (line.Contains("\t\t"))
        {
            line = line.Replace("\t\t", "\t");
        }
        var parts = line.Split('\t');
        if (parts.Length < 5)
        {
            logger.LogError("Line {LineNumber}: expected 5 tab-separated fields, got {Count} in '{Line}'.", lineNumber, parts.Length, line);
            return null;
        }

        if (!DateOnly.TryParseExact(parts[0].Trim(), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var accountingDate))
        {
            logger.LogError("Line {LineNumber}: cannot parse AccountingDate '{Value}'.", lineNumber, parts[0].Trim());
            return null;
        }

        if (!DateOnly.TryParseExact(parts[1].Trim(), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var transactionDate))
        {
            logger.LogError("Line {LineNumber}: cannot parse TransactionDate '{Value}'.", lineNumber, parts[1].Trim());
            return null;
        }

        var description = parts[2].Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            logger.LogError("Line {LineNumber}: Description is empty.", lineNumber);
            return null;
        }

        try
        {
            var amount = ParseSwedishDecimal(parts[3]);
            var balance = ParseSwedishDecimal(parts[4]);
            return new ParsedLine(lineNumber, accountingDate, transactionDate, description, amount, balance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Line {LineNumber}: failed to parse Amount/Balance from '{Line}'.", lineNumber, line);
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
