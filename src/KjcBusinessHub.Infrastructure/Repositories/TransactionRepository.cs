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
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

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
        => await db.Transactions.FirstOrDefaultAsync(
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

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken);
}
