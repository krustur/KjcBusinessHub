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
        // xUnit creates a new class instance per test, so each test gets its own isolated in-memory database.
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
        Assert.Single(found);
        Assert.Equal(doc.Id, found[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_includes_linked_transactions()
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 7, 1),
            Amount = 123.45m,
            Balance = 1000m,
            Description = "Linked transaction",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var doc = new SourceDocument
        {
            Id = Guid.NewGuid(),
            FileSubPath = "2026-07/2026-07-01 Invoice.pdf",
            FileHash = "linked",
            FileNameDate = new DateOnly(2026, 7, 1),
            Description = "Invoice",
            Status = SourceDocumentStatus.Active,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        doc.Transactions.Add(tx);

        await _db.SourceDocuments.AddAsync(doc);
        await _db.Transactions.AddAsync(tx);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();

        Assert.Single(all);
        Assert.Single(all[0].Transactions, linked => linked.Id == tx.Id);
    }

    [Fact]
    public async Task IsFutureTransaction_persists_and_round_trips()
    {
        var doc = new SourceDocument
        {
            Id = Guid.NewGuid(),
            FileSubPath = "2026-08/2026-08-01 Future Invoice.pdf",
            FileHash = "future123",
            FileNameDate = new DateOnly(2026, 8, 1),
            Description = "Future Invoice",
            Status = SourceDocumentStatus.New,
            IsFutureTransaction = true,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(doc);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();
        Assert.Single(all);
        Assert.True(all[0].IsFutureTransaction);
    }

    [Fact]
    public async Task IsFutureTransaction_defaults_to_false()
    {
        var doc = new SourceDocument
        {
            Id = Guid.NewGuid(),
            FileSubPath = "2026-08/2026-08-02 Normal Invoice.pdf",
            FileHash = "normal456",
            FileNameDate = new DateOnly(2026, 8, 2),
            Description = "Normal Invoice",
            Status = SourceDocumentStatus.New,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(doc);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();
        Assert.Single(all);
        Assert.False(all[0].IsFutureTransaction);
    }

    public void Dispose() => _db.Dispose();
}
