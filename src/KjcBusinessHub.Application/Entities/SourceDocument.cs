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
    public List<Transaction> Transactions { get; set; } = [];
}
