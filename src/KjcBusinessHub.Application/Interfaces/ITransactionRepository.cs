using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<bool> ExactMatchExistsAsync(DateOnly accountingDate, DateOnly transactionDate, TransactionType transactionType, string description, decimal amount, CancellationToken cancellationToken = default);
    Task<Transaction?> FindExactMatchAsync(DateOnly accountingDate, DateOnly transactionDate, TransactionType transactionType, string description, decimal amount, CancellationToken cancellationToken = default);
    Task LinkDocumentAsync(Guid transactionId, Guid sourceDocumentId, CancellationToken cancellationToken = default);
    Task UnlinkDocumentAsync(Guid transactionId, Guid sourceDocumentId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
