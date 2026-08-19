using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public DateOnly AccountingDate { get; set; }
    public DateOnly TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsHandledWithoutDocument { get; set; } = false;
    public ICollection<SourceDocument> SourceDocuments { get; private set; } = [];

    public int LinkedSourceDocumentCount => SourceDocuments.Count;

    public bool IsLinked => LinkedSourceDocumentCount > 0;

    public bool HasMultipleLinkedSourceDocuments => LinkedSourceDocumentCount > 1;

    public bool IsHandled => IsLinked || IsHandledWithoutDocument;

    public bool CanMarkAsHandledWithoutDocument => !IsLinked && !IsHandledWithoutDocument;

    public bool CanRemoveHandledMark => IsHandledWithoutDocument;

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
