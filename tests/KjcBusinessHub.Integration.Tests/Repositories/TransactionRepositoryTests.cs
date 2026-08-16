using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Infrastructure.Data;
using KjcBusinessHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KjcBusinessHub.Integration.Tests.Repositories;

public class TransactionRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TransactionRepository _repository;

    public TransactionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repository = new TransactionRepository(_db);
    }

    [Fact]
    public async Task AddAsync_and_GetAllAsync_round_trips()
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 7, 1),
            Amount = 1234.56m,
            Balance = 5000m,
            Description = "Test",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(tx);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(tx.Id, all[0].Id);
        Assert.Equal("Test", all[0].Description);
    }

    [Fact]
    public async Task ExactMatchExistsAsync_returns_true_for_matching_transaction()
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 7, 1),
            Amount = 1234.56m,
            Balance = 5000m,
            Description = "Test",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(tx);
        await _repository.SaveChangesAsync();

        var exists = await _repository.ExactMatchExistsAsync(
            new DateOnly(2026, 7, 31),
            new DateOnly(2026, 7, 1),
            "Test",
            1234.56m,
            5000m);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExactMatchExistsAsync_returns_false_for_non_matching_transaction()
    {
        var exists = await _repository.ExactMatchExistsAsync(
            new DateOnly(2026, 7, 31),
            new DateOnly(2026, 7, 1),
            "NonExistent",
            1234.56m,
            5000m);

        Assert.False(exists);
    }

    [Fact]
    public async Task UpdateAsync_persists_status_change()
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 7, 1),
            Amount = 100m,
            Balance = 1000m,
            Description = "Test",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(tx);
        await _repository.SaveChangesAsync();

        tx.Status = TransactionStatus.RemovedFromFile;
        await _repository.UpdateAsync(tx);
        await _repository.SaveChangesAsync();

        var fetched = await _repository.GetByIdAsync(tx.Id);
        Assert.NotNull(fetched);
        Assert.Equal(TransactionStatus.RemovedFromFile, fetched.Status);
    }

    [Fact]
    public async Task LinkDocumentAsync_allows_one_document_to_be_linked_to_multiple_transactions()
    {
        var doc = new SourceDocument
        {
            Id = Guid.NewGuid(),
            FileSubPath = "2026-07/2026-07-01 Receipt.pdf",
            FileHash = "abc123",
            FileNameDate = new DateOnly(2026, 7, 1),
            Description = "Receipt",
            Status = SourceDocumentStatus.Active,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var firstTx = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 7, 1),
            Amount = 40m,
            Balance = 1000m,
            Description = "First",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var secondTx = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 7, 2),
            Amount = 60m,
            Balance = 940m,
            Description = "Second",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _db.SourceDocuments.AddAsync(doc);
        await _repository.AddAsync(firstTx);
        await _repository.AddAsync(secondTx);
        await _repository.SaveChangesAsync();

        await _repository.LinkDocumentAsync(firstTx.Id, doc.Id);
        await _repository.LinkDocumentAsync(secondTx.Id, doc.Id);
        await _repository.SaveChangesAsync();

        var all = (await _repository.GetAllAsync())
            .Where(transaction => transaction.Id == firstTx.Id || transaction.Id == secondTx.Id)
            .OrderBy(transaction => transaction.Description)
            .ToList();

        Assert.Collection(
            all,
            transaction => Assert.Single(transaction.SourceDocuments, linked => linked.Id == doc.Id),
            transaction => Assert.Single(transaction.SourceDocuments, linked => linked.Id == doc.Id));
    }

    public void Dispose() => _db.Dispose();
}
