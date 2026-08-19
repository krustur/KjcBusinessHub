using System.Globalization;
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
    [GeneratedRegex("^\"([^\"]*)\";\"([^\"]*)\";\"([^\"]*)\";\"([^\"]*)\";\"([^\"]*)\"$", RegexOptions.Compiled)]
    private static partial Regex QuotedImportLine();

    private static readonly IReadOnlyDictionary<string, TransactionType> TransactionTypeMappings =
        new Dictionary<string, TransactionType>(StringComparer.Ordinal)
        {
            ["Överföring"] = TransactionType.Transfer,
            ["Kortköp"] = TransactionType.CardPurchase,
            ["BG-insättning"] = TransactionType.BankgiroDeposit,
            ["Autogiro"] = TransactionType.DirectDebit,
            ["Betalning"] = TransactionType.Payment,
            ["Insättning"] = TransactionType.Deposit,
            ["Årsavgift"] = TransactionType.AnnualFee,
            ["Skatteåterbäring"] = TransactionType.TaxRefund,
            ["Kontantinsättning"] = TransactionType.CashDeposit,
        };

    public async Task<TransactionImportPreviewResult> PreviewImportAsync(
        string pastedText,
        CancellationToken cancellationToken = default)
    {
        var existingTransactions = await repository.GetAllAsync(cancellationToken);
        var existingKeys = new HashSet<string>(
            existingTransactions.Select(existing => MakeKey(
                existing.AccountingDate,
                existing.TransactionDate,
                existing.TransactionType,
                existing.Description,
                existing.Amount)));

        var errorRows = new List<TransactionImportParseError>();
        var newTransactions = new List<TransactionImportPreviewTransaction>();
        var duplicateTransactions = new List<TransactionImportPreviewTransaction>();
        var newKeysInPreview = new HashSet<string>();

        var normalizedText = pastedText.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalizedText.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var rawLine = lines[index];
            var lineNumber = index + 1;

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            if (!TryParseLine(rawLine, lineNumber, out var parsedTransaction, out var parseError))
            {
                errorRows.Add(parseError!);
                logger.LogError(
                    "Line {LineNumber}: unable to parse transaction from: '{Line}'.",
                    lineNumber,
                    rawLine);
                continue;
            }

            var key = MakeKey(
                parsedTransaction!.AccountingDate,
                parsedTransaction.TransactionDate,
                parsedTransaction.TransactionType,
                parsedTransaction.Description,
                parsedTransaction.Amount);

            if (existingKeys.Contains(key))
            {
                duplicateTransactions.Add(parsedTransaction with { DuplicateReason = "Already exists in the app" });
                logger.LogDebug(
                    "Line {LineNumber}: transaction already exists, skipping import preview: {Description}.",
                    lineNumber,
                    parsedTransaction.Description);
                continue;
            }

            if (!newKeysInPreview.Add(key))
            {
                duplicateTransactions.Add(parsedTransaction with { DuplicateReason = "Duplicate row in pasted input" });
                logger.LogDebug(
                    "Line {LineNumber}: duplicate transaction within pasted input: {Description}.",
                    lineNumber,
                    parsedTransaction.Description);
                continue;
            }

            newTransactions.Add(parsedTransaction);
        }

        return new TransactionImportPreviewResult(errorRows, newTransactions, duplicateTransactions);
    }

    public async Task<TransactionImportCommitResult> ImportAsync(
        IReadOnlyList<TransactionImportPreviewTransaction> transactions,
        CancellationToken cancellationToken = default)
    {
        var importedCount = 0;
        var duplicateImportedCount = transactions.Count(transaction => !string.IsNullOrWhiteSpace(transaction.DuplicateReason));

        foreach (var previewTransaction in transactions)
        {
            await repository.AddAsync(
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountingDate = previewTransaction.AccountingDate,
                    TransactionDate = previewTransaction.TransactionDate,
                    TransactionType = previewTransaction.TransactionType,
                    Description = previewTransaction.Description,
                    Amount = previewTransaction.Amount,
                    Status = TransactionStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                cancellationToken);

            importedCount++;
            logger.LogInformation(
                "Line {LineNumber}: added transaction: {Description}.",
                previewTransaction.LineNumber,
                previewTransaction.Description);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return new TransactionImportCommitResult(importedCount, duplicateImportedCount);
    }

    private static decimal ParseSwedishDecimal(string value)
    {
        var normalized = value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);

        return decimal.Parse(normalized, CultureInfo.InvariantCulture);
    }

    private static string MakeKey(
        DateOnly accountingDate,
        DateOnly transactionDate,
        TransactionType transactionType,
        string description,
        decimal amount)
    {
        return $"{accountingDate}|{transactionDate}|{transactionType}|{description}|{amount:F2}";
    }

    private bool TryParseLine(
        string rawLine,
        int lineNumber,
        out TransactionImportPreviewTransaction? transaction,
        out TransactionImportParseError? error)
    {
        var match = QuotedImportLine().Match(rawLine.Trim());
        if (!match.Success)
        {
            transaction = null;
            error = new TransactionImportParseError(
                lineNumber,
                rawLine,
                $"Line {lineNumber}, unable to parse transaction from: '{rawLine}'");
            return false;
        }

        if (!DateOnly.TryParseExact(
                match.Groups[1].Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var accountingDate))
        {
            transaction = null;
            error = new TransactionImportParseError(
                lineNumber,
                rawLine,
                $"Line {lineNumber}, invalid AccountingDate: '{match.Groups[1].Value}'");
            return false;
        }

        if (!DateOnly.TryParseExact(
                match.Groups[2].Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var transactionDate))
        {
            transaction = null;
            error = new TransactionImportParseError(
                lineNumber,
                rawLine,
                $"Line {lineNumber}, invalid TransactionDate: '{match.Groups[2].Value}'");
            return false;
        }

        var swedishTransactionType = match.Groups[3].Value.Trim();
        if (!TransactionTypeMappings.TryGetValue(swedishTransactionType, out var transactionType))
        {
            transaction = null;
            error = new TransactionImportParseError(
                lineNumber,
                rawLine,
                $"Line {lineNumber}, invalid TransactionType: '{swedishTransactionType}'");
            return false;
        }

        var description = match.Groups[4].Value.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            transaction = null;
            error = new TransactionImportParseError(
                lineNumber,
                rawLine,
                $"Line {lineNumber}, description is required");
            return false;
        }

        try
        {
            var amount = ParseSwedishDecimal(match.Groups[5].Value);
            transaction = new TransactionImportPreviewTransaction(
                lineNumber,
                accountingDate,
                transactionDate,
                transactionType,
                swedishTransactionType,
                description,
                amount,
                null);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            transaction = null;
            error = new TransactionImportParseError(
                lineNumber,
                rawLine,
                $"Line {lineNumber}, invalid Amount: '{match.Groups[5].Value}'");
            logger.LogError(ex, "Line {LineNumber}: failed to parse amount from '{Line}'.", lineNumber, rawLine);
            return false;
        }
    }
}

public sealed record TransactionImportPreviewResult(
    IReadOnlyList<TransactionImportParseError> ErrorRows,
    IReadOnlyList<TransactionImportPreviewTransaction> NewTransactions,
    IReadOnlyList<TransactionImportPreviewTransaction> DuplicateTransactions);

public sealed record TransactionImportParseError(
    int LineNumber,
    string RawText,
    string Message);

public sealed record TransactionImportPreviewTransaction(
    int LineNumber,
    DateOnly AccountingDate,
    DateOnly TransactionDate,
    TransactionType TransactionType,
    string TransactionTypeLabel,
    string Description,
    decimal Amount,
    string? DuplicateReason)
{
    public string TransactionTypeDisplay => TransactionType switch
    {
        TransactionType.Transfer => "Transfer",
        TransactionType.CardPurchase => "Card Purchase",
        TransactionType.BankgiroDeposit => "Bankgiro Deposit",
        TransactionType.DirectDebit => "Direct Debit",
        TransactionType.Payment => "Payment",
        TransactionType.Deposit => "Deposit",
        TransactionType.AnnualFee => "Annual Fee",
        TransactionType.TaxRefund => "Tax Refund",
        TransactionType.CashDeposit => "Cash Deposit",
        _ => TransactionType.ToString(),
    };
}

public sealed record TransactionImportCommitResult(
    int ImportedCount,
    int DuplicateImportedCount);
