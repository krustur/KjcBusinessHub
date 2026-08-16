using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public DateOnly AccountingDate { get; set; }
    public DateOnly TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<SourceDocument> SourceDocuments { get; set; } = [];
}
