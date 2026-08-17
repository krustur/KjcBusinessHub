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

    public ObservableCollection<Transaction> AvailableTransactions { get; } = [];
    public ObservableCollection<SourceDocument> AvailableSourceDocuments { get; } = [];
    public ObservableCollection<LinkedTransactionGroup> LinkedTransactionGroups { get; } = [];
    public IReadOnlyList<SourceDocumentCurrency> SupportedCurrencies { get; } =
        Enum.GetValues<SourceDocumentCurrency>();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkDocumentCommand))]
    public partial Transaction? SelectedAvailableTransaction { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkDocumentCommand))]
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
    [NotifyPropertyChangedFor(nameof(IsSeeMonthMode))]
    [NotifyPropertyChangedFor(nameof(IsSeeAllMode))]
    [NotifyPropertyChangedFor(nameof(ShowAllMonths))]
    public partial FilterMode FilterMode { get; set; } = FilterMode.SeeMonth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today).Month;

    [ObservableProperty]
    public partial bool IncludeNeighbouringMonths { get; set; } = false;

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

    public bool IsSeeAllMode => FilterMode == FilterMode.SeeAll;
    public bool IsSeeMonthMode => FilterMode == FilterMode.SeeMonth;
    public bool ShowAllMonths
    {
        get => FilterMode == FilterMode.SeeAll;
        set
        {
            var nextMode = value ? FilterMode.SeeAll : FilterMode.SeeMonth;
            if (FilterMode == nextMode)
                return;

            FilterMode = nextMode;
            _ = RefreshAsync();
        }
    }

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
    }

    public async Task InitialiseAsync()
    {
        IsLoading = true;
        StatusMessage = "Importing data…";
        try
        {
            var folder = _settings.SourceDocumentFolder!;
            await _transactionImportService.ImportAsync(folder);
            await _sourceDocumentImportService.ImportAsync(folder);
            _fileWatcherService.Start();
            await RefreshAsync();
            StatusMessage = "Ready.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during import: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // --- Filter commands ---

    [RelayCommand]
    private async Task SetSeeAllAsync()
    {
        FilterMode = FilterMode.SeeAll;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task SetSeeMonthAsync()
    {
        FilterMode = FilterMode.SeeMonth;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToThisMonthAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        SelectedYear = today.Year;
        SelectedMonth = today.Month;
        FilterMode = FilterMode.SeeMonth;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToPreviousMonthAsync()
    {
        var current = new DateOnly(SelectedYear, SelectedMonth, 1).AddMonths(-1);
        SelectedYear = current.Year;
        SelectedMonth = current.Month;
        FilterMode = FilterMode.SeeMonth;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToNextMonthAsync()
    {
        var current = new DateOnly(SelectedYear, SelectedMonth, 1).AddMonths(1);
        SelectedYear = current.Year;
        SelectedMonth = current.Month;
        FilterMode = FilterMode.SeeMonth;
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

    partial void OnIncludeNeighbouringMonthsChanged(bool value) =>
        _ = RefreshAsync();

    partial void OnUseSeparateSourceDocumentMonthChanged(bool value) =>
        _ = RefreshAsync();

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
            await _transactionRepository.LinkDocumentAsync(
                SelectedAvailableTransaction.Id,
                SelectedAvailableSourceDocument.Id);
            await _transactionRepository.SaveChangesAsync();
            SelectedAvailableTransaction = null;
            SelectedAvailableSourceDocument = null;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error linking document: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error unlinking document: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open document: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ShowInExplorer(SourceDocument doc)
    {
        try
        {
            var fullPath = _fileSystemService.GetFullPath(_settings.SourceDocumentFolder!, doc.FileSubPath);
            _fileSystemService.ShowInExplorer(fullPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not show file in file manager: {ex.Message}";
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
                StatusMessage = string.Join(" ", validationResult.Errors.Select(error => error.Message));
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving amount: {ex.Message}";
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
    public partial int TransactionTotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    public partial int TransactionHandledCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    public partial int SourceDocumentTotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    public partial int SourceDocumentHandledCount { get; set; }

    public bool IsMonthComplete =>
        TransactionTotalCount > 0 &&
        SourceDocumentTotalCount > 0 &&
        TransactionHandledCount == TransactionTotalCount &&
        SourceDocumentHandledCount == SourceDocumentTotalCount;

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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error marking document as future: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing future mark from document: {ex.Message}";
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
            .ThenBy(d => d.Description);
        foreach (var doc in filteredDocs)
        {
            AvailableSourceDocuments.Add(doc);
        }

        // Monthly coverage counts
        TransactionTotalCount = monthFilteredTransactions.Count;
        TransactionHandledCount = monthFilteredTransactions.Count(t => t.IsLinked);

        var coverageDocs = ApplyDocumentMonthFilter(visibleDocs)
            .Where(d => !d.IsFutureTransaction)
            .ToList();
        SourceDocumentTotalCount = coverageDocs.Count;
        SourceDocumentHandledCount = coverageDocs.Count(d => d.IsLinked);
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

        StatusMessage = $"Invalid {fieldName}. Please enter a valid number.";
        value = null;
        return false;
    }

    private IEnumerable<Transaction> ApplyTransactionMonthFilter(IEnumerable<Transaction> transactions)
    {
        if (FilterMode == FilterMode.SeeAll)
            return transactions;

        return transactions.Where(t => IsInMonthRange(t.TransactionDate, SelectedYear, SelectedMonth));
    }

    private IEnumerable<SourceDocument> ApplyDocumentMonthFilter(IEnumerable<SourceDocument> docs)
    {
        if (FilterMode == FilterMode.SeeAll)
            return docs;

        var selectedYear = UseSeparateSourceDocumentMonth ? SelectedSourceDocumentYear : SelectedYear;
        var selectedMonth = UseSeparateSourceDocumentMonth ? SelectedSourceDocumentMonth : SelectedMonth;
        return docs.Where(d => IsInMonthRange(d.FileNameDate, selectedYear, selectedMonth));
    }

    private bool IsInMonthRange(DateOnly date, int selectedYear, int selectedMonth)
    {
        var selected = new DateOnly(selectedYear, selectedMonth, 1);

        if (IncludeNeighbouringMonths)
        {
            var prev = selected.AddMonths(-1);
            var next = selected.AddMonths(1);
            return (date.Year == prev.Year && date.Month == prev.Month) ||
                   (date.Year == selected.Year && date.Month == selected.Month) ||
                   (date.Year == next.Year && date.Month == next.Month);
        }

        return date.Year == selectedYear && date.Month == selectedMonth;
    }
}

public enum FilterMode
{
    SeeAll,
    SeeMonth,
}

public sealed record LinkedPair(Transaction Transaction, SourceDocument SourceDocument);

public sealed record LinkedTransactionGroup(
    Transaction Transaction,
    IReadOnlyList<LinkedPair> LinkedDocuments);
