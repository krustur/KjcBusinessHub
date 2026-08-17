using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Entities;

public class SourceDocument
{
    public Guid Id { get; set; }
    public string FileSubPath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public DateOnly FileNameDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public SourceDocumentCurrency? Ccy { get; set; }
    public decimal? CcyAmount { get; set; }
    public SourceDocumentStatus Status { get; set; } = SourceDocumentStatus.New;
    public DateTimeOffset FileCreatedDate { get; set; }
    public DateTimeOffset FileModifiedDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<Transaction> Transactions { get; private set; } = [];

    public string CurrencyDisplay =>
        Ccy.HasValue && CcyAmount.HasValue
            ? $"{Ccy.Value} {CcyAmount.Value:N2}"
            : string.Empty;

    public int LinkedTransactionCount => Transactions.Count;

    public bool IsLinked => LinkedTransactionCount > 0;

    public bool HasMultipleLinkedTransactions => LinkedTransactionCount > 1;

    public bool IsFutureTransaction { get; set; } = false;

    public SourceDocumentAnnualType AnnualType { get; set; } = SourceDocumentAnnualType.NotAnnual;

    public bool IsAnnual => AnnualType == SourceDocumentAnnualType.Annual;

    public bool IsOldAnnual => AnnualType == SourceDocumentAnnualType.OldAnnual;
}
