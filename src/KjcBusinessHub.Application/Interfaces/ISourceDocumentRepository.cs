using KjcBusinessHub.Application.Entities;

namespace KjcBusinessHub.Application.Interfaces;

public interface ISourceDocumentRepository
{
    Task<IReadOnlyList<SourceDocument>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SourceDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SourceDocument?> FindByFileSubPathAsync(string fileSubPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SourceDocument>> FindByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);
    Task AddAsync(SourceDocument sourceDocument, CancellationToken cancellationToken = default);
    Task UpdateAsync(SourceDocument sourceDocument, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
