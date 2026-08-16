using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KjcBusinessHub.Infrastructure.Repositories;

public class SourceDocumentRepository(AppDbContext db) : ISourceDocumentRepository
{
    public async Task<IReadOnlyList<SourceDocument>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.SourceDocuments
            .Include(d => d.Transactions)
            .ToListAsync(cancellationToken);

    public async Task<SourceDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.SourceDocuments
            .Include(d => d.Transactions)
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<SourceDocument?> FindByFileSubPathAsync(string fileSubPath, CancellationToken cancellationToken = default)
        => await db.SourceDocuments.SingleOrDefaultAsync(
            d => d.FileSubPath == fileSubPath, cancellationToken);

    public async Task<IReadOnlyList<SourceDocument>> FindByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
        => await db.SourceDocuments
            .Where(d => d.FileHash == fileHash)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SourceDocument sourceDocument, CancellationToken cancellationToken = default)
        => await db.SourceDocuments.AddAsync(sourceDocument, cancellationToken);

    public Task UpdateAsync(SourceDocument sourceDocument, CancellationToken cancellationToken = default)
    {
        db.SourceDocuments.Update(sourceDocument);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken);
}
