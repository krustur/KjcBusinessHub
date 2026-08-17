using System.Collections.Generic;
using System.Threading.Tasks;
using KjcBusinessHub.Application.Entities;
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
