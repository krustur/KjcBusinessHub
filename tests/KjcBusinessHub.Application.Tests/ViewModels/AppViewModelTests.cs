using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using KjcBusinessHub.Application.Validators;
using KjcBusinessHub.UI.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace KjcBusinessHub.Application.Tests.ViewModels;

public class AppViewModelTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ISourceDocumentRepository _sourceDocumentRepository = Substitute.For<ISourceDocumentRepository>();
    private readonly ISettingsService _settings = Substitute.For<ISettingsService>();
    private readonly IFileSystemService _fileSystemService = Substitute.For<IFileSystemService>();

    public AppViewModelTests()
    {
        _transactionRepository.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<Transaction>>([]));
        _sourceDocumentRepository.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<SourceDocument>>([]));
    }

    [Fact]
    public void Disabling_sync_keeps_the_current_transaction_month_for_source_documents()
    {
        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;

        sut.SyncTransactionAndSourceDocumentMonth = false;

        Assert.True(sut.UseSeparateSourceDocumentMonth);
        Assert.Equal(2026, sut.SelectedSourceDocumentYear);
        Assert.Equal(8, sut.SelectedSourceDocumentMonth);
    }

    [Fact]
    public async Task Sync_with_transaction_command_is_enabled_only_when_months_differ_in_separate_mode()
    {
        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.SyncTransactionAndSourceDocumentMonth = false;

        Assert.False(sut.SyncSourceDocumentMonthWithTransactionCommand.CanExecute(null));

        sut.SelectedSourceDocumentMonth = 7;
        Assert.True(sut.SyncSourceDocumentMonthWithTransactionCommand.CanExecute(null));

        await sut.SyncSourceDocumentMonthWithTransactionCommand.ExecuteAsync(null);

        Assert.Equal(8, sut.SelectedSourceDocumentMonth);
        Assert.False(sut.SyncSourceDocumentMonthWithTransactionCommand.CanExecute(null));

        sut.SelectedSourceDocumentYear = 2025;
        Assert.True(sut.SyncSourceDocumentMonthWithTransactionCommand.CanExecute(null));

        await sut.SyncSourceDocumentMonthWithTransactionCommand.ExecuteAsync(null);

        Assert.Equal(2026, sut.SelectedSourceDocumentYear);
        Assert.False(sut.SyncSourceDocumentMonthWithTransactionCommand.CanExecute(null));
    }

    [Fact]
    public async Task Future_marked_documents_are_excluded_from_source_document_coverage_totals()
    {
        var month = new DateOnly(2026, 8, 1);
        var normalDoc = MakeActiveDoc(Guid.NewGuid(), month, isFuture: false);
        var futureDoc = MakeActiveDoc(Guid.NewGuid(), month, isFuture: true);

        _sourceDocumentRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<SourceDocument>>([normalDoc, futureDoc]));

        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.FilterMode = FilterMode.SeeMonth;
        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(1, sut.SourceDocumentTotalCount);
        Assert.Equal(0, sut.SourceDocumentHandledCount);
    }

    [Fact]
    public async Task Future_marked_documents_remain_visible_in_available_source_documents()
    {
        var month = new DateOnly(2026, 8, 1);
        var futureDoc = MakeActiveDoc(Guid.NewGuid(), month, isFuture: true);

        _sourceDocumentRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<SourceDocument>>([futureDoc]));

        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.FilterMode = FilterMode.SeeMonth;
        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.Single(sut.AvailableSourceDocuments);
        Assert.True(sut.AvailableSourceDocuments[0].IsFutureTransaction);
    }

    [Fact]
    public async Task IsMonthComplete_is_false_when_there_are_unhandled_documents()
    {
        var month = new DateOnly(2026, 8, 1);
        var doc = MakeActiveDoc(Guid.NewGuid(), month, isFuture: false);

        _sourceDocumentRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<SourceDocument>>([doc]));

        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.FilterMode = FilterMode.SeeMonth;
        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.False(sut.IsMonthComplete);
    }

    [Fact]
    public async Task IsMonthComplete_is_false_when_only_future_documents_exist()
    {
        var month = new DateOnly(2026, 8, 1);
        var futureDoc = MakeActiveDoc(Guid.NewGuid(), month, isFuture: true);

        _sourceDocumentRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<SourceDocument>>([futureDoc]));

        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.FilterMode = FilterMode.SeeMonth;
        await sut.RefreshCommand.ExecuteAsync(null);

        // SourceDocumentTotalCount == 0 means month cannot be considered complete
        Assert.Equal(0, sut.SourceDocumentTotalCount);
        Assert.False(sut.IsMonthComplete);
    }

    [Fact]
    public async Task Linking_a_pending_document_to_a_transaction_clears_the_pending_flag()
    {
        var month = new DateOnly(2026, 8, 1);
        var futureDoc = MakeActiveDoc(Guid.NewGuid(), month, isFuture: true);
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = month,
            TransactionDate = month,
            Amount = 100m,
            Balance = 0m,
            Description = "Test",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _transactionRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<Transaction>>([transaction]));
        _sourceDocumentRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<SourceDocument>>([futureDoc]));

        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.FilterMode = FilterMode.SeeMonth;
        await sut.RefreshCommand.ExecuteAsync(null);

        sut.SelectedAvailableTransaction = transaction;
        sut.SelectedAvailableSourceDocument = futureDoc;
        await sut.LinkDocumentCommand.ExecuteAsync(null);

        Assert.False(futureDoc.IsFutureTransaction);
        await _sourceDocumentRepository.Received(1).UpdateAsync(futureDoc);
    }

    private static SourceDocument MakeActiveDoc(Guid id, DateOnly fileNameDate, bool isFuture) =>
        new()
        {
            Id = id,
            FileSubPath = $"{fileNameDate:yyyy-MM}/{fileNameDate:yyyy-MM-dd} Invoice.pdf",
            FileHash = id.ToString(),
            FileNameDate = fileNameDate,
            Description = "Invoice",
            Amount = 100m,
            Status = SourceDocumentStatus.Active,
            IsFutureTransaction = isFuture,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private AppViewModel CreateSubject()
    {
        var transactionImportService = new TransactionImportService(
            _transactionRepository,
            NullLogger<TransactionImportService>.Instance);
        var sourceDocumentImportService = new SourceDocumentImportService(
            _sourceDocumentRepository,
            NullLogger<SourceDocumentImportService>.Instance);
        var fileWatcherService = new FileWatcherService(
            transactionImportService,
            sourceDocumentImportService,
            _settings,
            NullLogger<FileWatcherService>.Instance);

        return new AppViewModel(
            _transactionRepository,
            _sourceDocumentRepository,
            transactionImportService,
            sourceDocumentImportService,
            fileWatcherService,
            _settings,
            _fileSystemService,
            new SourceDocumentValidator());
    }
}
