using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KjcBusinessHub.Infrastructure.Repositories;

public class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.Transactions
            .Include(t => t.SourceDocuments)
            .ToListAsync(cancellationToken);

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.Transactions
            .Include(t => t.SourceDocuments)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<bool> ExactMatchExistsAsync(
        DateOnly accountingDate, DateOnly transactionDate, string description,
        decimal amount, decimal balance, CancellationToken cancellationToken = default)
        => await db.Transactions.AnyAsync(
            t => t.AccountingDate == accountingDate &&
                 t.TransactionDate == transactionDate &&
                 t.Description == description &&
                 t.Amount == amount &&
                 t.Balance == balance,
            cancellationToken);

    public async Task<Transaction?> FindExactMatchAsync(
        DateOnly accountingDate, DateOnly transactionDate, string description,
        decimal amount, decimal balance, CancellationToken cancellationToken = default)
        => await db.Transactions.SingleOrDefaultAsync(
            t => t.AccountingDate == accountingDate &&
                 t.TransactionDate == transactionDate &&
                 t.Description == description &&
                 t.Amount == amount &&
                 t.Balance == balance,
            cancellationToken);

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
        => await db.Transactions.AddAsync(transaction, cancellationToken);

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        db.Transactions.Update(transaction);
        return Task.CompletedTask;
    }

    public async Task LinkDocumentAsync(Guid transactionId, Guid sourceDocumentId, CancellationToken cancellationToken = default)
    {
        var transaction = await db.Transactions
            .Include(t => t.SourceDocuments)
            .SingleOrDefaultAsync(t => t.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        var doc = await db.SourceDocuments
            .SingleOrDefaultAsync(d => d.Id == sourceDocumentId, cancellationToken)
            ?? throw new InvalidOperationException($"SourceDocument {sourceDocumentId} not found.");

        if (!transaction.SourceDocuments.Any(d => d.Id == sourceDocumentId))
            transaction.SourceDocuments.Add(doc);
    }

    public async Task UnlinkDocumentAsync(Guid transactionId, Guid sourceDocumentId, CancellationToken cancellationToken = default)
    {
        var transaction = await db.Transactions
            .Include(t => t.SourceDocuments)
            .SingleOrDefaultAsync(t => t.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        var doc = transaction.SourceDocuments.FirstOrDefault(d => d.Id == sourceDocumentId)
            ?? throw new InvalidOperationException($"SourceDocument {sourceDocumentId} is not linked to Transaction {transactionId}.");
        transaction.SourceDocuments.Remove(doc);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken);
}
