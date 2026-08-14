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

    public void Dispose() => _db.Dispose();
}
