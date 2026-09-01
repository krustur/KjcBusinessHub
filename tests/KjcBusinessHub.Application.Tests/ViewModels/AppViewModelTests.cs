using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    public void Selecting_a_different_source_document_month_switches_to_separate_month_mode()
    {
        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;

        sut.SelectedSourceDocumentMonthOption = new MonthOption(new DateOnly(2026, 7, 1));

        Assert.True(sut.UseSeparateSourceDocumentMonth);
        Assert.Equal(2026, sut.SelectedSourceDocumentYear);
        Assert.Equal(7, sut.SelectedSourceDocumentMonth);
    }

    [Fact]
    public void Selecting_the_transaction_month_for_source_documents_keeps_separate_mode_until_sync_is_explicitly_enabled()
    {
        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.SyncTransactionAndSourceDocumentMonth = false;
        sut.SelectedSourceDocumentYear = 2026;
        sut.SelectedSourceDocumentMonth = 7;

        sut.SelectedSourceDocumentMonthOption = new MonthOption(new DateOnly(2026, 8, 1));

        Assert.True(sut.UseSeparateSourceDocumentMonth);
        Assert.False(sut.SyncTransactionAndSourceDocumentMonth);
        Assert.Equal(2026, sut.SelectedSourceDocumentYear);
        Assert.Equal(8, sut.SelectedSourceDocumentMonth);
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
    public async Task Future_marked_documents_are_visible_even_when_viewing_a_different_month()
    {
        var docMonth = new DateOnly(2026, 7, 1);
        var futureDoc = MakeActiveDoc(Guid.NewGuid(), docMonth, isFuture: true);

        _sourceDocumentRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<SourceDocument>>([futureDoc]));

        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8; // viewing August, but the document is dated July
        sut.FilterMode = FilterMode.SeeMonth;
        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.Single(sut.AvailableSourceDocuments);
        Assert.True(sut.AvailableSourceDocuments[0].IsFutureTransaction);
    }

    [Fact]
    public void Show_source_document_month_in_folder_uses_the_effective_month_folder()
    {
        _settings.SourceDocumentFolder.Returns("/source-documents");

        var sut = CreateSubject();
        sut.SelectedYear = 2025;
        sut.SelectedMonth = 10;

        sut.ShowSourceDocumentMonthInExplorerCommand.Execute(null);

        _fileSystemService.Received(1).ShowInExplorer(Path.Combine("/source-documents", "2025-10"));
    }

    [Fact]
    public async Task Available_source_documents_sort_pending_then_annual_before_other_documents()
    {
        var selectedMonth = new DateOnly(2026, 8, 1);
        var pendingDoc = MakeActiveDoc(Guid.NewGuid(), new DateOnly(2026, 7, 1), isFuture: true);
        pendingDoc.Description = "Pending";

        var annualDoc = MakeActiveDoc(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            isFuture: false,
            annualType: SourceDocumentAnnualType.Annual);
        annualDoc.Description = "Annual";
        annualDoc.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = selectedMonth,
            TransactionDate = selectedMonth,
            TransactionType = TransactionType.Payment,
            Amount = 100m,
            Description = "Linked",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var regularDoc = MakeActiveDoc(Guid.NewGuid(), selectedMonth, isFuture: false);
        regularDoc.Description = "Regular";

        _sourceDocumentRepository.GetAllAsync().Returns(
            Task.FromResult<IReadOnlyList<SourceDocument>>([regularDoc, annualDoc, pendingDoc]));

        var sut = CreateSubject();
        sut.SelectedYear = 2026;
        sut.SelectedMonth = 8;
        sut.FilterMode = FilterMode.SeeMonth;
        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(
            ["Pending", "Annual", "Regular"],
            sut.AvailableSourceDocuments.Select(doc => doc.Description).ToArray());
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
            TransactionType = TransactionType.Payment,
            Amount = 100m,
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

    [Fact]
    public async Task Not_annual_documents_can_be_marked_annual_but_not_expired_annual()
    {
        var doc = MakeActiveDoc(Guid.NewGuid(), new DateOnly(2026, 8, 1), isFuture: false);
        var sut = CreateSubject();

        Assert.True(doc.CanMarkAsAnnual);
        Assert.False(doc.CanMarkAsExpiredAnnual);
        Assert.False(doc.CanClearAnnualType);

        await sut.MarkAsExpiredAnnualCommand.ExecuteAsync(doc);

        Assert.Equal(SourceDocumentAnnualType.NotAnnual, doc.AnnualType);
        await _sourceDocumentRepository.DidNotReceive().UpdateAsync(doc);

        await sut.MarkAsAnnualCommand.ExecuteAsync(doc);

        Assert.Equal(SourceDocumentAnnualType.Annual, doc.AnnualType);
        await _sourceDocumentRepository.Received(1).UpdateAsync(doc);
    }

    [Fact]
    public async Task Annual_documents_can_be_cleared_or_marked_expired_annual()
    {
        var doc = MakeActiveDoc(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 1),
            isFuture: false,
            annualType: SourceDocumentAnnualType.Annual);
        var sut = CreateSubject();

        Assert.False(doc.CanMarkAsAnnual);
        Assert.True(doc.CanMarkAsExpiredAnnual);
        Assert.True(doc.CanClearAnnualType);

        await sut.MarkAsExpiredAnnualCommand.ExecuteAsync(doc);

        Assert.Equal(SourceDocumentAnnualType.ExpiredAnnual, doc.AnnualType);

        await sut.ClearAnnualTypeCommand.ExecuteAsync(doc);

        Assert.Equal(SourceDocumentAnnualType.NotAnnual, doc.AnnualType);
        await _sourceDocumentRepository.Received(2).UpdateAsync(doc);
    }

    [Fact]
    public async Task Expired_annual_documents_can_be_marked_annual_or_cleared()
    {
        var annualDoc = MakeActiveDoc(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 1),
            isFuture: false,
            annualType: SourceDocumentAnnualType.ExpiredAnnual);
        var clearDoc = MakeActiveDoc(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 1),
            isFuture: false,
            annualType: SourceDocumentAnnualType.ExpiredAnnual);
        var sut = CreateSubject();

        Assert.True(annualDoc.CanMarkAsAnnual);
        Assert.False(annualDoc.CanMarkAsExpiredAnnual);
        Assert.True(annualDoc.CanClearAnnualType);

        await sut.MarkAsAnnualCommand.ExecuteAsync(annualDoc);

        Assert.Equal(SourceDocumentAnnualType.Annual, annualDoc.AnnualType);

        Assert.True(clearDoc.CanClearAnnualType);

        await sut.ClearAnnualTypeCommand.ExecuteAsync(clearDoc);

        Assert.Equal(SourceDocumentAnnualType.NotAnnual, clearDoc.AnnualType);
        await _sourceDocumentRepository.Received(1).UpdateAsync(annualDoc);
        await _sourceDocumentRepository.Received(1).UpdateAsync(clearDoc);
    }

    [Fact]
    public async Task Transaction_import_preview_updates_new_duplicate_and_error_groups()
    {
        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 8, 16),
            TransactionDate = new DateOnly(2026, 8, 16),
            TransactionType = TransactionType.Transfer,
            Amount = -82000.00m,
            Description = "9060.42.850.51",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _transactionRepository.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<Transaction>>([existing]));

        var sut = CreateSubject();
        sut.OpenTransactionImportCommand.Execute(null);
        sut.TransactionImportText =
            """
            "2026-08-16";"2026-08-16";"Överföring";"9060.42.850.51";"-82 000,00"
            "2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            invalid row
            """;

        await Task.Delay(50);

        Assert.Single(sut.NewTransactionImports);
        Assert.Single(sut.DuplicateTransactionImports);
        Assert.Single(sut.TransactionImportErrorRows);
        Assert.True(sut.HasTransactionImportErrors);
    }

    [Fact]
    public async Task Transaction_import_requires_error_acknowledgement_before_import()
    {
        var sut = CreateSubject();
        sut.OpenTransactionImportCommand.Execute(null);
        sut.TransactionImportText =
            """
            "2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            invalid row
            """;

        await Task.Delay(50);

        Assert.False(sut.ImportTransactionsCommand.CanExecute(null));

        sut.HasAcknowledgedTransactionImportErrors = true;

        Assert.True(sut.ImportTransactionsCommand.CanExecute(null));
    }

    [Fact]
    public async Task Transaction_import_requires_explicit_duplicate_decisions_before_import()
    {
        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 8, 16),
            TransactionDate = new DateOnly(2026, 8, 16),
            TransactionType = TransactionType.Transfer,
            Amount = -82000.00m,
            Description = "9060.42.850.51",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _transactionRepository.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<Transaction>>([existing]));

        var sut = CreateSubject();
        sut.OpenTransactionImportCommand.Execute(null);
        sut.TransactionImportText =
            """
            "2026-08-16";"2026-08-16";"Överföring";"9060.42.850.51";"-82 000,00"
            "2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
            """;

        await Task.Delay(50);

        Assert.False(sut.ImportTransactionsCommand.CanExecute(null));

        sut.DuplicateTransactionImports[0].SelectedDecisionOption =
            sut.DuplicateTransactionImports[0].DecisionOptions[1];

        Assert.True(sut.ImportTransactionsCommand.CanExecute(null));
    }

    [Fact]
    public async Task Import_transactions_command_imports_selected_duplicates_and_closes_window()
    {
        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountingDate = new DateOnly(2026, 8, 16),
            TransactionDate = new DateOnly(2026, 8, 16),
            TransactionType = TransactionType.Transfer,
            Amount = -82000.00m,
            Description = "9060.42.850.51",
            Status = TransactionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _transactionRepository.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<Transaction>>([existing]));

        var sut = CreateSubject();
        sut.OpenTransactionImportCommand.Execute(null);
        sut.TransactionImportText =
            """
            "2026-08-16";"2026-08-16";"Överföring";"9060.42.850.51";"-82 000,00"
            """;

        await Task.Delay(50);
        sut.DuplicateTransactionImports[0].SelectedDecisionOption =
            sut.DuplicateTransactionImports[0].DecisionOptions[0];

        await sut.ImportTransactionsCommand.ExecuteAsync(null);

        await _transactionRepository.Received(1).AddAsync(
            Arg.Is<Transaction>(transaction =>
                transaction.TransactionType == TransactionType.Transfer &&
                transaction.Description == "9060.42.850.51" &&
                transaction.Amount == -82000.00m));
        await _transactionRepository.Received(1).SaveChangesAsync();
        Assert.False(sut.IsTransactionImportOpen);
        Assert.Empty(sut.NewTransactionImports);
        Assert.Empty(sut.DuplicateTransactionImports);
    }

    private static SourceDocument MakeActiveDoc(
        Guid id,
        DateOnly fileNameDate,
        bool isFuture,
        SourceDocumentAnnualType annualType = SourceDocumentAnnualType.NotAnnual) =>
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
            AnnualType = annualType,
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
