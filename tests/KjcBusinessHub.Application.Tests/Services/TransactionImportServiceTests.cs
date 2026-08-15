using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace KjcBusinessHub.Application.Tests.Services;

public class TransactionImportServiceTests
{
    private readonly ITransactionRepository _repository;
    private readonly TransactionImportService _service;

    public TransactionImportServiceTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _service = new TransactionImportService(_repository, NullLogger<TransactionImportService>.Instance);
    }

    [Fact]
    public async Task First_import_adds_all_transactions()
    {
        var lines = new[]
        {
            "2026-07-31\t2026-06-08\tClient payment\t5 000,00\t45 050,18",
            "2026-07-31\t2026-07-01\tOffice supplies\t-499,00\t44 551,18",
        };

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([]));

        await _service.ProcessLinesAsync(lines);

        await _repository.Received(2).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_transaction_is_skipped()
    {
        var lines = new[]
        {
            "2026-07-31\t2026-06-08\tClient payment\t5 000,00\t45 050,18",
        };

        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 6, 8),
            Description = "Client payment",
            Amount = 5000m,
            Balance = 45050.18m,
            Status = TransactionStatus.Active,
        };

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([existing]));

        await _service.ProcessLinesAsync(lines);

        await _repository.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transaction_removed_from_file_is_marked_RemovedFromFile()
    {
        // No lines in file but one existing active transaction
        var lines = Array.Empty<string>();

        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 6, 8),
            Description = "Client payment",
            Amount = 5000m,
            Balance = 45050.18m,
            Status = TransactionStatus.Active,
        };

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([existing]));

        await _service.ProcessLinesAsync(lines);

        Assert.Equal(TransactionStatus.RemovedFromFile, existing.Status);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transaction_with_checkbox_prefix_is_parsed()
    {
        var lines = new[]
        {
            "[X] 2026-07-31\t2026-06-08\tClient payment\t5 000,00\t45 050,18",
        };

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([]));

        await _service.ProcessLinesAsync(lines);

        await _repository.Received(1).AddAsync(
            Arg.Is<Transaction>(t => t.Description == "Client payment" && t.Amount == 5000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_lines_are_skipped()
    {
        var lines = new[]
        {
            "# This is a comment",
            "2026-07-31\t2026-06-08\tClient payment\t5 000,00\t45 050,18 # inline comment",
        };

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([]));

        await _service.ProcessLinesAsync(lines);

        await _repository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovedFromFile_transaction_is_reactivated_when_present_in_file_again()
    {
        var lines = new[]
        {
            "2026-07-31\t2026-06-08\tClient payment\t5 000,00\t45 050,18",
        };

        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 7, 31),
            TransactionDate = new DateOnly(2026, 6, 8),
            Description = "Client payment",
            Amount = 5000m,
            Balance = 45050.18m,
            Status = TransactionStatus.RemovedFromFile,
        };

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([existing]));

        await _service.ProcessLinesAsync(lines);

        Assert.Equal(TransactionStatus.Active, existing.Status);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }
}
