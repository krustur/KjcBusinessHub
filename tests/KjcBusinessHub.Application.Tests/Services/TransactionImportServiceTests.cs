using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

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
    public async Task PreviewImportAsync_parses_new_transactions()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([]));

        var result = await _service.PreviewImportAsync(
            """
            "2026-08-16";"2026-08-16";"Överföring";"9060.42.850.51";"-82 000,00"
            "2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            """);

        Assert.Empty(result.ErrorRows);
        Assert.Empty(result.DuplicateTransactions);
        Assert.Collection(
            result.NewTransactions,
            first =>
            {
                Assert.Equal(new DateOnly(2026, 8, 16), first.AccountingDate);
                Assert.Equal(new DateOnly(2026, 8, 16), first.TransactionDate);
                Assert.Equal(TransactionType.Transfer, first.TransactionType);
                Assert.Equal("9060.42.850.51", first.Description);
                Assert.Equal(-82000.00m, first.Amount);
            },
            second =>
            {
                Assert.Equal(TransactionType.CardPurchase, second.TransactionType);
                Assert.Equal("MAXI ICA STORMARKNAD U,OREBRO,SE", second.Description);
                Assert.Equal(-34.95m, second.Amount);
            });
    }

    [Fact]
    public async Task PreviewImportAsync_classifies_existing_transactions_as_duplicates()
    {
        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 8, 16),
            TransactionDate = new DateOnly(2026, 8, 16),
            TransactionType = TransactionType.Transfer,
            Description = "9060.42.850.51",
            Amount = -82000.00m,
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([existing]));

        var result = await _service.PreviewImportAsync(
            """
            "2026-08-16";"2026-08-16";"Överföring";"9060.42.850.51";"-82 000,00"
            "2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            """);

        Assert.Single(result.NewTransactions);
        Assert.Single(result.DuplicateTransactions);
        Assert.Equal("Already exists in the app", result.DuplicateTransactions[0].DuplicateReason);
    }

    [Fact]
    public async Task PreviewImportAsync_returns_error_rows_for_invalid_lines()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([]));

        var result = await _service.PreviewImportAsync(
            """
            not a valid transaction
            "2026-08-09";"bad-date";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            """);

        Assert.Empty(result.NewTransactions);
        Assert.Empty(result.DuplicateTransactions);
        Assert.Equal(2, result.ErrorRows.Count);
        Assert.Contains(result.ErrorRows, row => row.LineNumber == 1);
        Assert.Contains(result.ErrorRows, row => row.LineNumber == 2);
    }

    [Fact]
    public async Task PreviewImportAsync_marks_repeated_rows_in_same_input_as_duplicates()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Transaction>>([]));

        var result = await _service.PreviewImportAsync(
            """
            "2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            "2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            """);

        Assert.Single(result.NewTransactions);
        Assert.Single(result.DuplicateTransactions);
        Assert.Equal("Duplicate row in pasted input", result.DuplicateTransactions[0].DuplicateReason);
    }

    [Fact]
    public async Task ImportAsync_adds_all_selected_transactions_including_duplicates()
    {
        var previewTransactions = new[]
        {
            new TransactionImportPreviewTransaction(
                1,
                new DateOnly(2026, 8, 16),
                new DateOnly(2026, 8, 16),
                TransactionType.Transfer,
                "Överföring",
                "9060.42.850.51",
                -82000.00m,
                "Already exists in the app"),
            new TransactionImportPreviewTransaction(
                2,
                new DateOnly(2026, 8, 9),
                new DateOnly(2026, 8, 8),
                TransactionType.CardPurchase,
                "Kortköp",
                "MAXI ICA STORMARKNAD U,OREBRO,SE",
                -34.95m,
                null),
        };

        var result = await _service.ImportAsync(previewTransactions);

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.DuplicateImportedCount);
        await _repository.Received(2).AddAsync(
            Arg.Any<Transaction>(),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
