using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Infrastructure.Data;
using KjcBusinessHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KjcBusinessHub.Integration.Tests.Repositories;

public class SourceDocumentRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SourceDocumentRepository _repository;

    public SourceDocumentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repository = new SourceDocumentRepository(_db);
    }

    [Fact]
    public async Task AddAsync_and_GetAllAsync_round_trips()
    {
        var doc = new SourceDocument
        {
            Id = Guid.NewGuid(),
            FileSubPath = "2026-07/2026-07-01 Invoice.pdf",
            FileHash = "abc123",
            FileNameDate = new DateOnly(2026, 7, 1),
            Description = "Invoice",
            Status = SourceDocumentStatus.New,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(doc);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(doc.Id, all[0].Id);
    }

    [Fact]
    public async Task FindByFileSubPathAsync_returns_matching_document()
    {
        var doc = new SourceDocument
        {
            Id = Guid.NewGuid(),
            FileSubPath = "2026-07/2026-07-01 Invoice.pdf",
            FileHash = "abc123",
            FileNameDate = new DateOnly(2026, 7, 1),
            Description = "Invoice",
            Status = SourceDocumentStatus.New,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(doc);
        await _repository.SaveChangesAsync();

        var found = await _repository.FindByFileSubPathAsync("2026-07/2026-07-01 Invoice.pdf");
        Assert.NotNull(found);
        Assert.Equal(doc.Id, found.Id);
    }

    [Fact]
    public async Task FindByFileHashAsync_returns_matching_document()
    {
        var doc = new SourceDocument
        {
            Id = Guid.NewGuid(),
            FileSubPath = "2026-07/2026-07-01 Invoice.pdf",
            FileHash = "deadbeef",
            FileNameDate = new DateOnly(2026, 7, 1),
            Description = "Invoice",
            Status = SourceDocumentStatus.New,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(doc);
        await _repository.SaveChangesAsync();

        var found = await _repository.FindByFileHashAsync("deadbeef");
        Assert.NotNull(found);
    }

    public void Dispose() => _db.Dispose();
}
