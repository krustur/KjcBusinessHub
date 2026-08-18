using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using KjcBusinessHub.Application.Validators;

namespace KjcBusinessHub.UI.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISourceDocumentRepository _sourceDocumentRepository;
    private readonly TransactionImportService _transactionImportService;
    private readonly SourceDocumentImportService _sourceDocumentImportService;
    private readonly FileWatcherService _fileWatcherService;
    private readonly ISettingsService _settings;
    private readonly IFileSystemService _fileSystemService;
    private readonly SourceDocumentValidator _sourceDocumentValidator;
    private bool _hasInitialised;
    private bool _isInitialising;

    public ObservableCollection<Transaction> AvailableTransactions { get; } = [];
    public ObservableCollection<SourceDocument> AvailableSourceDocuments { get; } = [];
    public ObservableCollection<LinkedTransactionGroup> LinkedTransactionGroups { get; } = [];
    public IReadOnlyList<SourceDocumentCurrency> SupportedCurrencies { get; } =
        Enum.GetValues<SourceDocumentCurrency>();
    public IReadOnlyList<ViewScopeOption> ViewScopeOptions { get; } =
    [
        new(MonthViewScope.CurrentMonth, "Current month", "Show only items from the selected month."),
        new(MonthViewScope.AdjacentMonths, "Current + adjacent months", "Include the previous and next month for easier review."),
        new(MonthViewScope.AllMonths, "All months", "Show the full history without month filtering."),
    ];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial StatusTone StatusTone { get; set; } = StatusTone.Info;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkDocumentCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedTransactionSummary))]
    [NotifyPropertyChangedFor(nameof(LinkingHint))]
    public partial Transaction? SelectedAvailableTransaction { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkDocumentCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentSummary))]
    [NotifyPropertyChangedFor(nameof(LinkingHint))]
    public partial SourceDocument? SelectedAvailableSourceDocument { get; set; }

    // --- Set Amount inline editing ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSettingAmount))]
    public partial SourceDocument? DocumentBeingAmounted { get; set; }

    [ObservableProperty]
    public partial string AmountInputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelectCurrency))]
    public partial string CcyAmountInputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SourceDocumentCurrency? SelectedCurrency { get; set; }

    public bool IsSettingAmount => DocumentBeingAmounted is not null;
    public bool CanSelectCurrency => !string.IsNullOrWhiteSpace(CcyAmountInputText);

    // --- Filter state ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthNavigationEnabled))]
    public partial ViewScopeOption SelectedViewScopeOption { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today).Month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthLabel))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedSourceDocumentYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthLabel))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedSourceDocumentMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today).Month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncTransactionAndSourceDocumentMonth))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial bool UseSeparateSourceDocumentMonth { get; set; } = false;

    public bool IsMonthNavigationEnabled => SelectedViewScope != MonthViewScope.AllMonths;
    public MonthViewScope SelectedViewScope => SelectedViewScopeOption.Scope;

    public bool SyncTransactionAndSourceDocumentMonth
    {
        get => !UseSeparateSourceDocumentMonth;
        set
        {
            var useSeparateMonth = !value;
            if (UseSeparateSourceDocumentMonth == useSeparateMonth)
                return;

            if (!value)
            {
                SelectedSourceDocumentYear = SelectedYear;
                SelectedSourceDocumentMonth = SelectedMonth;
            }

            UseSeparateSourceDocumentMonth = useSeparateMonth;
        }
    }

    public string SelectedMonthLabel =>
        new DateOnly(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");

    public string SelectedSourceDocumentMonthLabel =>
        new DateOnly(SelectedSourceDocumentYear, SelectedSourceDocumentMonth, 1).ToString("MMMM yyyy");

    public string SelectedTransactionSummary =>
        SelectedAvailableTransaction is null
            ? "Choose a transaction from the list."
            : $"{SelectedAvailableTransaction.TransactionDate:dd.MM.yyyy} • {SelectedAvailableTransaction.Description} • {SelectedAvailableTransaction.Amount:N2}";

    public string SelectedSourceDocumentSummary =>
        SelectedAvailableSourceDocument is null
            ? "Choose a source document from the list."
            : $"{SelectedAvailableSourceDocument.FileNameDate:dd.MM.yyyy} • {SelectedAvailableSourceDocument.Description} • {SelectedAvailableSourceDocument.Amount?.ToString("N2") ?? "No amount"}";

    public string LinkingHint
    {
        get
        {
            if (SelectedAvailableTransaction is null && SelectedAvailableSourceDocument is null)
                return "Select a transaction and a source document to create a link.";

            if (SelectedAvailableTransaction is null)
                return "Select a transaction to continue.";

            if (SelectedAvailableSourceDocument is null)
                return "Select a source document to continue.";

            return SelectedAvailableSourceDocument.Status == SourceDocumentStatus.Active
                ? "Ready to link the selected items."
                : "Only active source documents can be linked.";
        }
    }

    public bool HasAvailableTransactions => AvailableTransactions.Count > 0;
    public bool HasAvailableSourceDocuments => AvailableSourceDocuments.Count > 0;
    public bool HasLinkedTransactionGroups => LinkedTransactionGroups.Count > 0;

    public double TransactionCoveragePercent =>
        TransactionTotalCount == 0 ? 0 : (double)TransactionHandledCount / TransactionTotalCount * 100;

    public double SourceDocumentCoveragePercent =>
        SourceDocumentTotalCount == 0 ? 0 : (double)SourceDocumentHandledCount / SourceDocumentTotalCount * 100;

    public AppViewModel(
        ITransactionRepository transactionRepository,
        ISourceDocumentRepository sourceDocumentRepository,
        TransactionImportService transactionImportService,
        SourceDocumentImportService sourceDocumentImportService,
        FileWatcherService fileWatcherService,
        ISettingsService settings,
        IFileSystemService fileSystemService,
        SourceDocumentValidator sourceDocumentValidator)
    {
        _transactionRepository = transactionRepository;
        _sourceDocumentRepository = sourceDocumentRepository;
        _transactionImportService = transactionImportService;
        _sourceDocumentImportService = sourceDocumentImportService;
        _fileWatcherService = fileWatcherService;
        _settings = settings;
        _fileSystemService = fileSystemService;
        _sourceDocumentValidator = sourceDocumentValidator;
        SelectedViewScopeOption = ViewScopeOptions[0];
    }

    public async Task InitialiseAsync()
    {
        if (_isInitialising)
            return;

        if (_hasInitialised)
        {
            await RefreshAsync();
            return;
        }

        _isInitialising = true;
        IsLoading = true;
        SetStatus("Importing data…", StatusTone.Info);
        try
        {
            var folder = _settings.SourceDocumentFolder!;
            await _transactionImportService.ImportAsync(folder);
            await _sourceDocumentImportService.ImportAsync(folder);
            _fileWatcherService.Start();
            _hasInitialised = true;
            await RefreshAsync();
            SetStatus("Workspace ready. Review the summary cards and start matching documents.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error during import: {ex.Message}", StatusTone.Error);
        }
        finally
        {
            IsLoading = false;
            _isInitialising = false;
        }
    }

    [RelayCommand]
    private async Task GoToThisMonthAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        SelectedYear = today.Year;
        SelectedMonth = today.Month;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToPreviousMonthAsync()
    {
        var current = new DateOnly(SelectedYear, SelectedMonth, 1).AddMonths(-1);
        SelectedYear = current.Year;
        SelectedMonth = current.Month;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToNextMonthAsync()
    {
        var current = new DateOnly(SelectedYear, SelectedMonth, 1).AddMonths(1);
        SelectedYear = current.Year;
        SelectedMonth = current.Month;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToSourceDocumentThisMonthAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        SelectedSourceDocumentYear = today.Year;
        SelectedSourceDocumentMonth = today.Month;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToSourceDocumentPreviousMonthAsync()
    {
        var current = new DateOnly(SelectedSourceDocumentYear, SelectedSourceDocumentMonth, 1).AddMonths(-1);
        SelectedSourceDocumentYear = current.Year;
        SelectedSourceDocumentMonth = current.Month;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToSourceDocumentNextMonthAsync()
    {
        var current = new DateOnly(SelectedSourceDocumentYear, SelectedSourceDocumentMonth, 1).AddMonths(1);
        SelectedSourceDocumentYear = current.Year;
        SelectedSourceDocumentMonth = current.Month;
        await RefreshAsync();
    }

    private bool CanSyncSourceDocumentMonthWithTransaction() =>
        UseSeparateSourceDocumentMonth &&
        (SelectedSourceDocumentYear != SelectedYear || SelectedSourceDocumentMonth != SelectedMonth);

    [RelayCommand(CanExecute = nameof(CanSyncSourceDocumentMonthWithTransaction))]
    private async Task SyncSourceDocumentMonthWithTransactionAsync()
    {
        SelectedSourceDocumentYear = SelectedYear;
        SelectedSourceDocumentMonth = SelectedMonth;
        await RefreshAsync();
    }

    partial void OnSelectedViewScopeOptionChanged(ViewScopeOption value)
    {
        OnPropertyChanged(nameof(SelectedViewScope));
        _ = RefreshAsync();
    }

    partial void OnUseSeparateSourceDocumentMonthChanged(bool value) =>
        _ = RefreshAsync();

    partial void OnSelectedAvailableTransactionChanged(Transaction? value)
    {
        OnPropertyChanged(nameof(SelectedTransactionSummary));
        OnPropertyChanged(nameof(LinkingHint));
    }

    partial void OnSelectedAvailableSourceDocumentChanged(SourceDocument? value)
    {
        OnPropertyChanged(nameof(SelectedSourceDocumentSummary));
        OnPropertyChanged(nameof(LinkingHint));
    }

    // --- Linking commands ---

    private bool CanLinkDocument() =>
        SelectedAvailableTransaction is not null &&
        SelectedAvailableSourceDocument is not null &&
        SelectedAvailableSourceDocument.Status == SourceDocumentStatus.Active;

    [RelayCommand(CanExecute = nameof(CanLinkDocument))]
    private async Task LinkDocumentAsync()
    {
        if (SelectedAvailableTransaction is null || SelectedAvailableSourceDocument is null)
            return;

        try
        {
            var doc = SelectedAvailableSourceDocument;
            await _transactionRepository.LinkDocumentAsync(
                SelectedAvailableTransaction.Id,
                doc.Id);

            if (doc.IsFutureTransaction)
            {
                doc.IsFutureTransaction = false;
                doc.UpdatedAt = DateTimeOffset.UtcNow;
                await _sourceDocumentRepository.UpdateAsync(doc);
            }

            await _transactionRepository.SaveChangesAsync();
            SelectedAvailableTransaction = null;
            SelectedAvailableSourceDocument = null;
            await RefreshAsync();
            SetStatus("Linked the selected transaction and source document.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error linking document: {ex.Message}", StatusTone.Error);
        }
    }

    [RelayCommand]
    private async Task UnlinkDocumentAsync(LinkedPair pair)
    {
        try
        {
            await _transactionRepository.UnlinkDocumentAsync(
                pair.Transaction.Id,
                pair.SourceDocument.Id);
            await _transactionRepository.SaveChangesAsync();
            await RefreshAsync();
            SetStatus("Removed the document link.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error unlinking document: {ex.Message}", StatusTone.Error);
        }
    }

    // --- Source document actions (UC-0301 / UC-0302 / UC-0303) ---

    [RelayCommand]
    private void OpenDocument(SourceDocument doc)
    {
        try
        {
            var fullPath = _fileSystemService.GetFullPath(_settings.SourceDocumentFolder!, doc.FileSubPath);
            _fileSystemService.OpenFile(fullPath);
            SetStatus("Opened the selected document.", StatusTone.Info);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open document: {ex.Message}", StatusTone.Error);
        }
    }

    [RelayCommand]
    private void ShowInExplorer(SourceDocument doc)
    {
        try
        {
            var fullPath = _fileSystemService.GetFullPath(_settings.SourceDocumentFolder!, doc.FileSubPath);
            _fileSystemService.ShowInExplorer(fullPath);
            SetStatus("Opened the document location.", StatusTone.Info);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not show file in file manager: {ex.Message}", StatusTone.Error);
        }
    }

    [RelayCommand]
    private void BeginSetAmount(SourceDocument doc)
    {
        AmountInputText = doc.Amount.HasValue
            ? doc.Amount.Value.ToString("G", CultureInfo.InvariantCulture)
            : string.Empty;
        CcyAmountInputText = doc.CcyAmount.HasValue
            ? doc.CcyAmount.Value.ToString("G", CultureInfo.InvariantCulture)
            : string.Empty;
        SelectedCurrency = doc.Ccy;
        DocumentBeingAmounted = doc;
    }

    [RelayCommand]
    private void CancelSetAmount()
    {
        DocumentBeingAmounted = null;
        AmountInputText = string.Empty;
        CcyAmountInputText = string.Empty;
        SelectedCurrency = null;
    }

    [RelayCommand]
    private async Task ConfirmSetAmountAsync()
    {
        if (DocumentBeingAmounted is null)
            return;

        if (!TryParseOptionalAmount(AmountInputText, "amount", out var amount))
            return;

        if (!TryParseOptionalAmount(CcyAmountInputText, "currency amount", out var ccyAmount))
            return;

        try
        {
            var selectedCurrency = ccyAmount.HasValue ? SelectedCurrency : null;
            var candidate = new SourceDocument
            {
                Amount = amount,
                CcyAmount = ccyAmount,
                Ccy = selectedCurrency,
            };

            var validationResult = _sourceDocumentValidator.ValidateSetAmount(candidate);
            if (!validationResult.IsValid)
            {
                SetStatus(string.Join(" ", validationResult.Errors.Select(error => error.Message)), StatusTone.Error);
                return;
            }

            DocumentBeingAmounted.Amount = amount;
            DocumentBeingAmounted.CcyAmount = ccyAmount;
            DocumentBeingAmounted.Ccy = selectedCurrency;
            DocumentBeingAmounted.Status = SourceDocumentStatus.Active;
            DocumentBeingAmounted.UpdatedAt = DateTimeOffset.UtcNow;
            await _sourceDocumentRepository.UpdateAsync(DocumentBeingAmounted);
            await _sourceDocumentRepository.SaveChangesAsync();
            DocumentBeingAmounted = null;
            AmountInputText = string.Empty;
            CcyAmountInputText = string.Empty;
            SelectedCurrency = null;
            await RefreshAsync();
            SetStatus("Saved the document amount details.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error saving amount: {ex.Message}", StatusTone.Error);
        }
    }

    partial void OnCcyAmountInputTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SelectedCurrency = null;
        }
    }

    // --- Monthly coverage ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TransactionCoveragePercent))]
    public partial int TransactionTotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TransactionCoveragePercent))]
    public partial int TransactionHandledCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    [NotifyPropertyChangedFor(nameof(SourceDocumentCoveragePercent))]
    public partial int SourceDocumentTotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    [NotifyPropertyChangedFor(nameof(SourceDocumentCoveragePercent))]
    public partial int SourceDocumentHandledCount { get; set; }

    public bool IsMonthComplete =>
        TransactionTotalCount > 0 &&
        SourceDocumentTotalCount > 0 &&
        TransactionHandledCount == TransactionTotalCount &&
        SourceDocumentHandledCount == SourceDocumentTotalCount;

    // --- Mark Transaction as handled without a linked document ---

    [RelayCommand]
    private async Task MarkAsHandledWithoutDocumentAsync(Transaction tx)
    {
        try
        {
            tx.IsHandledWithoutDocument = true;
            tx.UpdatedAt = DateTimeOffset.UtcNow;
            await _transactionRepository.UpdateAsync(tx);
            await _transactionRepository.SaveChangesAsync();
            await RefreshAsync();
            SetStatus("Marked the transaction as handled without a document.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error marking transaction as handled: {ex.Message}", StatusTone.Error);
        }
    }

    [RelayCommand]
    private async Task UnmarkAsHandledWithoutDocumentAsync(Transaction tx)
    {
        try
        {
            tx.IsHandledWithoutDocument = false;
            tx.UpdatedAt = DateTimeOffset.UtcNow;
            await _transactionRepository.UpdateAsync(tx);
            await _transactionRepository.SaveChangesAsync();
            await RefreshAsync();
            SetStatus("Removed the handled-without-document mark.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error removing handled mark from transaction: {ex.Message}", StatusTone.Error);
        }
    }

    // --- Set SourceDocument annual classification ---

    [RelayCommand]
    private async Task MarkAsAnnualAsync(SourceDocument doc)
    {
        await SetAnnualTypeAsync(doc, SourceDocumentAnnualType.Annual, "marking document as annual");
    }

    [RelayCommand]
    private async Task MarkAsExpiredAnnualAsync(SourceDocument doc)
    {
        await SetAnnualTypeAsync(doc, SourceDocumentAnnualType.ExpiredAnnual, "marking document as expired annual");
    }

    [RelayCommand]
    private async Task ClearAnnualTypeAsync(SourceDocument doc)
    {
        await SetAnnualTypeAsync(doc, SourceDocumentAnnualType.NotAnnual, "clearing annual type");
    }

    private async Task SetAnnualTypeAsync(SourceDocument doc, SourceDocumentAnnualType annualType, string actionDescription)
    {
        if (!doc.CanTransitionAnnualTypeTo(annualType))
            return;

        try
        {
            doc.AnnualType = annualType;
            doc.UpdatedAt = DateTimeOffset.UtcNow;
            await _sourceDocumentRepository.UpdateAsync(doc);
            await _sourceDocumentRepository.SaveChangesAsync();
            await RefreshAsync();
            SetStatus("Updated the annual document status.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error {actionDescription}: {ex.Message}", StatusTone.Error);
        }
    }

    // --- Mark as Future Transaction (UC-0306 / UC-0307) ---

    [RelayCommand]
    private async Task MarkAsFutureTransactionAsync(SourceDocument doc)
    {
        try
        {
            doc.IsFutureTransaction = true;
            doc.UpdatedAt = DateTimeOffset.UtcNow;
            await _sourceDocumentRepository.UpdateAsync(doc);
            await _sourceDocumentRepository.SaveChangesAsync();
            await RefreshAsync();
            SetStatus("Marked the document as pending.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error marking document as future: {ex.Message}", StatusTone.Error);
        }
    }

    [RelayCommand]
    private async Task UnmarkAsFutureTransactionAsync(SourceDocument doc)
    {
        try
        {
            doc.IsFutureTransaction = false;
            doc.UpdatedAt = DateTimeOffset.UtcNow;
            await _sourceDocumentRepository.UpdateAsync(doc);
            await _sourceDocumentRepository.SaveChangesAsync();
            await RefreshAsync();
            SetStatus("Removed the pending mark from the document.", StatusTone.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Error removing future mark from document: {ex.Message}", StatusTone.Error);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var allTransactions = await _transactionRepository.GetAllAsync();
        var allDocs = await _sourceDocumentRepository.GetAllAsync();

        AvailableTransactions.Clear();
        LinkedTransactionGroups.Clear();
        AvailableSourceDocuments.Clear();

        var activeTransactions = allTransactions
            .Where(t => t.Status == TransactionStatus.Active)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var monthFilteredTransactions = ApplyTransactionMonthFilter(activeTransactions).ToList();
        var linkedTransactions = monthFilteredTransactions.Where(t => t.IsLinked).ToList();

        // Linked pairs grouped by transaction and sorted by transaction date, then document date.
        foreach (var tx in linkedTransactions)
        {
            LinkedTransactionGroups.Add(new LinkedTransactionGroup(
                tx,
                tx.SourceDocuments
                    .OrderBy(d => d.FileNameDate)
                    .Select(doc => new LinkedPair(tx, doc))
                    .ToList()));
        }

        // Available transactions — apply optional month filter and keep linked items below unlinked ones.
        var filteredTransactions = monthFilteredTransactions
            .OrderBy(t => t.IsLinked)
            .ThenBy(t => t.TransactionDate)
            .ThenBy(t => t.AccountingDate);
        foreach (var tx in filteredTransactions)
        {
            AvailableTransactions.Add(tx);
        }

        // Available source documents — apply optional month filter and keep linked items below unlinked ones.
        var visibleDocs = allDocs
            .Where(d =>
                d.Status != SourceDocumentStatus.Removed &&
                d.Status != SourceDocumentStatus.RemovedFromDisk)
            .OrderBy(d => d.FileNameDate)
            .ToList();

        var filteredDocs = ApplyDocumentMonthFilter(visibleDocs)
            .OrderBy(d => d.IsLinked)
            .ThenBy(d => d.FileNameDate)
            .ThenBy(d => d.Description)
            .ToList();
        foreach (var doc in filteredDocs)
        {
            AvailableSourceDocuments.Add(doc);
        }

        // Monthly coverage counts — future-marked documents are excluded from SourceDocument totals
        TransactionTotalCount = monthFilteredTransactions.Count;
        TransactionHandledCount = monthFilteredTransactions.Count(t => t.IsHandled);

        var coverageDocs = filteredDocs.Where(d => !d.IsFutureTransaction).ToList();
        SourceDocumentTotalCount = coverageDocs.Count;
        SourceDocumentHandledCount = coverageDocs.Count(d => d.IsLinked);

        OnPropertyChanged(nameof(HasAvailableTransactions));
        OnPropertyChanged(nameof(HasAvailableSourceDocuments));
        OnPropertyChanged(nameof(HasLinkedTransactionGroups));
    }

    private bool TryParseOptionalAmount(string input, string fieldName, out decimal? value)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            value = null;
            return true;
        }

        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out var parsedValue))
        {
            value = parsedValue;
            return true;
        }

        SetStatus($"Invalid {fieldName}. Please enter a valid number.", StatusTone.Error);
        value = null;
        return false;
    }

    private IEnumerable<Transaction> ApplyTransactionMonthFilter(IEnumerable<Transaction> transactions)
    {
        if (SelectedViewScope == MonthViewScope.AllMonths)
            return transactions;

        return transactions.Where(t => IsInMonthRange(t.TransactionDate, SelectedYear, SelectedMonth));
    }

    private IEnumerable<SourceDocument> ApplyDocumentMonthFilter(IEnumerable<SourceDocument> docs)
    {
        if (SelectedViewScope == MonthViewScope.AllMonths)
            return docs;

        var selectedYear = UseSeparateSourceDocumentMonth ? SelectedSourceDocumentYear : SelectedYear;
        var selectedMonth = UseSeparateSourceDocumentMonth ? SelectedSourceDocumentMonth : SelectedMonth;
        return docs.Where(d =>
            d.IsFutureTransaction ||
            d.IsAnnual ||
            IsInMonthRange(d.FileNameDate, selectedYear, selectedMonth));
    }

    private bool IsInMonthRange(DateOnly date, int selectedYear, int selectedMonth)
    {
        var selected = new DateOnly(selectedYear, selectedMonth, 1);

        if (SelectedViewScope == MonthViewScope.AdjacentMonths)
        {
            var prev = selected.AddMonths(-1);
            var next = selected.AddMonths(1);
            return (date.Year == prev.Year && date.Month == prev.Month) ||
                   (date.Year == selected.Year && date.Month == selected.Month) ||
                   (date.Year == next.Year && date.Month == next.Month);
        }

        return date.Year == selectedYear && date.Month == selectedMonth;
    }

    private void SetStatus(string message, StatusTone tone)
    {
        StatusTone = tone;
        StatusMessage = message;
    }
}

public enum MonthViewScope
{
    CurrentMonth,
    AdjacentMonths,
    AllMonths,
}

public enum StatusTone
{
    Info,
    Success,
    Warning,
    Error,
}

public sealed record ViewScopeOption(MonthViewScope Scope, string Label, string Description)
{
    public override string ToString() => Label;
}

public sealed record LinkedPair(Transaction Transaction, SourceDocument SourceDocument);

public sealed record LinkedTransactionGroup(
    Transaction Transaction,
    IReadOnlyList<LinkedPair> LinkedDocuments);
