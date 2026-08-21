using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
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
    public ObservableCollection<TransactionImportParseError> TransactionImportErrorRows { get; } = [];
    public ObservableCollection<TransactionImportPreviewTransaction> NewTransactionImports { get; } = [];
    public ObservableCollection<DuplicateTransactionImportItem> DuplicateTransactionImports { get; } = [];
    public IReadOnlyList<SourceDocumentCurrency> SupportedCurrencies { get; } =
        Enum.GetValues<SourceDocumentCurrency>();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    [NotifyPropertyChangedFor(nameof(HasStatusOrMonthComplete))]
    [NotifyPropertyChangedFor(nameof(IsStatusError))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusText))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusForeground))]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsTransactionImportOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportTransactionsCommand))]
    public partial string TransactionImportText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImportTransactions))]
    [NotifyCanExecuteChangedFor(nameof(ImportTransactionsCommand))]
    public partial bool HasAcknowledgedTransactionImportErrors { get; set; }

    [ObservableProperty]
    public partial string? TransactionImportSummary { get; set; }

    public bool HasTransactionImportErrors => TransactionImportErrorRows.Count > 0;

    public bool CanImportTransactions =>
        (NewTransactionImports.Count > 0 || DuplicateTransactionImports.Any(transaction => transaction.KeepTransaction)) &&
        DuplicateTransactionImports.All(transaction => transaction.HasDecision) &&
        (!HasTransactionImportErrors || HasAcknowledgedTransactionImportErrors);

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasStatusOrMonthComplete => HasStatusMessage || IsMonthComplete;

    public bool IsStatusError =>
        !string.IsNullOrWhiteSpace(StatusMessage) &&
        (StatusMessage.Contains("error", StringComparison.OrdinalIgnoreCase) ||
         StatusMessage.Contains("could not", StringComparison.OrdinalIgnoreCase) ||
         StatusMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase));

    public string TopBarStatusText =>
        HasStatusMessage
            ? StatusMessage!
            : IsMonthComplete
                ? "Month complete"
                : string.Empty;

    public string TopBarStatusForeground =>
        IsStatusError
            ? "Red"
            : IsMonthComplete
                ? "Green"
                : "Gray";

    public string TopBarBackground => IsMonthComplete ? "#DFF4E0" : "#EEF5FC";

    public string TopBarBorderBrush => IsMonthComplete ? "Green" : "Gray";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkDocumentCommand))]
    [NotifyPropertyChangedFor(nameof(HasBothSelected))]
    public partial Transaction? SelectedAvailableTransaction { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkDocumentCommand))]
    [NotifyPropertyChangedFor(nameof(HasBothSelected))]
    public partial SourceDocument? SelectedAvailableSourceDocument { get; set; }

    public bool HasBothSelected =>
        SelectedAvailableTransaction is not null && SelectedAvailableSourceDocument is not null;

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

    // All VM mutations happen on the Avalonia UI thread, so a simple bool
    // flag is sufficient to suppress re-entrant refreshes.
    private bool _suppressRefresh;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSeeMonthMode))]
    [NotifyPropertyChangedFor(nameof(IsSeeAllMode))]
    [NotifyPropertyChangedFor(nameof(ShowAllMonths))]
    [NotifyPropertyChangedFor(nameof(ViewScope))]
    [NotifyPropertyChangedFor(nameof(IsCurrentMonthScope))]
    [NotifyPropertyChangedFor(nameof(IsAdjacentMonthsScope))]
    [NotifyPropertyChangedFor(nameof(IsAllMonthsScope))]
    [NotifyPropertyChangedFor(nameof(IsMonthScopeVisible))]
    [NotifyPropertyChangedFor(nameof(IsShowDocumentsMonthSelector))]
    public partial FilterMode FilterMode { get; set; } = FilterMode.SeeMonth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedTransactionMonthOption))]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthOption))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedTransactionMonthOption))]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthOption))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today).Month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ViewScope))]
    [NotifyPropertyChangedFor(nameof(IsCurrentMonthScope))]
    [NotifyPropertyChangedFor(nameof(IsAdjacentMonthsScope))]
    [NotifyPropertyChangedFor(nameof(IsShowDocumentsMonthSelector))]
    public partial bool IncludeNeighbouringMonths { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthOption))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedSourceDocumentYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedSourceDocumentMonthOption))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial int SelectedSourceDocumentMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today).Month;

    // Available month dropdowns
    public ObservableCollection<MonthOption> AvailableTransactionMonths { get; } = [];
    public ObservableCollection<MonthOption> AvailableSourceDocumentMonths { get; } = [];

    public MonthOption? SelectedTransactionMonthOption
    {
        get => AvailableTransactionMonths.FirstOrDefault(m => m.Date.Year == SelectedYear && m.Date.Month == SelectedMonth);
        set
        {
            if (value is null) return;
            if (SelectedYear == value.Date.Year && SelectedMonth == value.Date.Month) return;
            SelectedYear = value.Date.Year;
            SelectedMonth = value.Date.Month;
            _ = RefreshAsync();
        }
    }

    public MonthOption? SelectedSourceDocumentMonthOption
    {
        get
        {
            var selected = GetEffectiveSourceDocumentMonth();
            return AvailableSourceDocumentMonths.FirstOrDefault(
                m => m.Date.Year == selected.Year && m.Date.Month == selected.Month);
        }
        set
        {
            if (value is null) return;
            SetSourceDocumentMonth(value.Date);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncTransactionAndSourceDocumentMonth))]
    [NotifyPropertyChangedFor(nameof(IsShowDocumentsMonthSelector))]
    [NotifyCanExecuteChangedFor(nameof(SyncSourceDocumentMonthWithTransactionCommand))]
    public partial bool UseSeparateSourceDocumentMonth { get; set; } = false;

    public bool IsSeeAllMode => FilterMode == FilterMode.SeeAll;
    public bool IsSeeMonthMode => FilterMode == FilterMode.SeeMonth;

    // --- View scope (collapses FilterMode + IncludeNeighbouringMonths into a single enum) ---

    public ViewScope ViewScope
    {
        get => FilterMode switch
        {
            FilterMode.SeeAll => ViewScope.AllMonths,
            FilterMode.SeeMonth when IncludeNeighbouringMonths => ViewScope.AdjacentMonths,
            _ => ViewScope.CurrentMonth,
        };
        set
        {
            var (newMode, newInclude) = value switch
            {
                ViewScope.AllMonths => (FilterMode.SeeAll, false),
                ViewScope.AdjacentMonths => (FilterMode.SeeMonth, true),
                _ => (FilterMode.SeeMonth, false),
            };

            if (FilterMode == newMode && IncludeNeighbouringMonths == newInclude)
                return;

            _suppressRefresh = true;
            FilterMode = newMode;
            IncludeNeighbouringMonths = newInclude;
            _suppressRefresh = false;
            _ = RefreshAsync();
        }
    }

    public bool IsCurrentMonthScope
    {
        get => ViewScope == ViewScope.CurrentMonth;
        set { if (value) ViewScope = ViewScope.CurrentMonth; }
    }

    public bool IsAdjacentMonthsScope
    {
        get => ViewScope == ViewScope.AdjacentMonths;
        set { if (value) ViewScope = ViewScope.AdjacentMonths; }
    }

    public bool IsAllMonthsScope
    {
        get => ViewScope == ViewScope.AllMonths;
        set { if (value) ViewScope = ViewScope.AllMonths; }
    }

    public bool IsMonthScopeVisible => ViewScope != ViewScope.AllMonths;
    public bool IsShowDocumentsMonthSelector => IsMonthScopeVisible;

    public bool ShowAllMonths
    {
        get => FilterMode == FilterMode.SeeAll;
        set => ViewScope = value ? ViewScope.AllMonths : ViewScope.CurrentMonth;
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

    [RelayCommand]
    private void ToggleSeparateDocumentMonth()
    {
        SyncTransactionAndSourceDocumentMonth = !SyncTransactionAndSourceDocumentMonth;
    }

    [RelayCommand]
    private void DismissStatusMessage()
    {
        StatusMessage = null;
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
        StatusMessage = "Loading data…";
        try
        {
            var folder = _settings.SourceDocumentFolder!;
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
        ApplySourceDocumentMonth(today);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToSourceDocumentPreviousMonthAsync()
    {
        var current = GetEffectiveSourceDocumentMonth().AddMonths(-1);
        ApplySourceDocumentMonth(current);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GoToSourceDocumentNextMonthAsync()
    {
        var current = GetEffectiveSourceDocumentMonth().AddMonths(1);
        ApplySourceDocumentMonth(current);
        await RefreshAsync();
    }

    [RelayCommand]
    private void ShowSourceDocumentMonthInExplorer()
    {
        try
        {
            var folder = Path.Combine(
                _settings.SourceDocumentFolder!,
                GetEffectiveSourceDocumentMonth().ToString("yyyy-MM", CultureInfo.InvariantCulture));
            _fileSystemService.ShowInExplorer(folder);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not show folder in file manager: {ex.Message}";
        }
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

    partial void OnIncludeNeighbouringMonthsChanged(bool value)
    {
        if (!_suppressRefresh) _ = RefreshAsync();
    }

    partial void OnUseSeparateSourceDocumentMonthChanged(bool value) =>
        _ = RefreshAsync();

    partial void OnTransactionImportTextChanged(string value)
    {
        _ = PreviewTransactionImportAsync(value);
    }

    /// <summary>Action invoked to navigate to the Calendar view. Wired by <see cref="MainWindowViewModel"/>.</summary>
    public Action? NavigateToCalendar { get; set; }

    [RelayCommand]
    private void OpenCalendar()
    {
        NavigateToCalendar?.Invoke();
    }

    [RelayCommand]
    private void OpenTransactionImport()
    {
        IsTransactionImportOpen = true;
        StatusMessage = null;
    }

    [RelayCommand]
    private void CloseTransactionImport()
    {
        IsTransactionImportOpen = false;
        TransactionImportText = string.Empty;
        HasAcknowledgedTransactionImportErrors = false;
        TransactionImportSummary = null;
        ClearTransactionImportPreview();
    }

    [RelayCommand(CanExecute = nameof(CanImportTransactions))]
    private async Task ImportTransactionsAsync()
    {
        try
        {
            var keptDuplicateTransactions = DuplicateTransactionImports
                .Where(transaction => transaction.KeepTransaction)
                .Select(transaction => transaction.ToPreviewTransaction())
                .ToList();
            var transactionsToImport = NewTransactionImports
                .Concat(keptDuplicateTransactions)
                .ToList();
            var result = await _transactionImportService.ImportAsync(transactionsToImport);

            var status = $"Imported {result.ImportedCount} transaction(s).";
            if (result.DuplicateImportedCount > 0)
            {
                status += $" Included {result.DuplicateImportedCount} user-approved duplicate transaction(s).";
            }

            StatusMessage = status;
            IsTransactionImportOpen = false;
            TransactionImportText = string.Empty;
            HasAcknowledgedTransactionImportErrors = false;
            TransactionImportSummary = null;
            ClearTransactionImportPreview();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing transactions: {ex.Message}";
        }
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
    [NotifyPropertyChangedFor(nameof(TopBarBackground))]
    [NotifyPropertyChangedFor(nameof(TopBarBorderBrush))]
    [NotifyPropertyChangedFor(nameof(HasStatusOrMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusText))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusForeground))]
    public partial int TransactionTotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TopBarBackground))]
    [NotifyPropertyChangedFor(nameof(TopBarBorderBrush))]
    [NotifyPropertyChangedFor(nameof(HasStatusOrMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusText))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusForeground))]
    public partial int TransactionHandledCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TopBarBackground))]
    [NotifyPropertyChangedFor(nameof(TopBarBorderBrush))]
    [NotifyPropertyChangedFor(nameof(HasStatusOrMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusText))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusForeground))]
    public partial int SourceDocumentTotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TopBarBackground))]
    [NotifyPropertyChangedFor(nameof(TopBarBorderBrush))]
    [NotifyPropertyChangedFor(nameof(HasStatusOrMonthComplete))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusText))]
    [NotifyPropertyChangedFor(nameof(TopBarStatusForeground))]
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error marking transaction as handled: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing handled mark from transaction: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error {actionDescription}: {ex.Message}";
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
            .OrderBy(d => GetSourceDocumentSortRank(d))
            .ThenBy(d => d.IsLinked)
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

        // Populate month dropdowns
        var today = DateOnly.FromDateTime(DateTime.Today);
        var upperBound = today.AddMonths(1);
        upperBound = new DateOnly(upperBound.Year, upperBound.Month, 1);

        var txMin = allTransactions.Select(t => t.TransactionDate).DefaultIfEmpty(today).Min();
        var txLower = new DateOnly(txMin.Year, txMin.Month, 1);

        var docMin = allDocs.Select(d => d.FileNameDate).DefaultIfEmpty(today).Min();
        var docLower = new DateOnly(docMin.Year, docMin.Month, 1);

        var sharedLower = txLower < docLower ? txLower : docLower;
        RebuildMonthOptions(AvailableTransactionMonths, sharedLower, upperBound);
        OnPropertyChanged(nameof(SelectedTransactionMonthOption));

        RebuildMonthOptions(AvailableSourceDocumentMonths, sharedLower, upperBound);
        OnPropertyChanged(nameof(SelectedSourceDocumentMonthOption));
    }

    private static void RebuildMonthOptions(ObservableCollection<MonthOption> collection, DateOnly from, DateOnly to)
    {
        var cursor = from;
        var newOptions = new List<MonthOption>();
        while (cursor <= to)
        {
            newOptions.Add(new MonthOption(cursor));
            cursor = cursor.AddMonths(1);
        }

        // Only rebuild if content changed to avoid unnecessary UI churn.
        if (collection.Count == newOptions.Count &&
            collection.Zip(newOptions).All(pair => pair.First.Date == pair.Second.Date))
            return;

        collection.Clear();
        foreach (var opt in newOptions)
            collection.Add(opt);
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

    private async Task PreviewTransactionImportAsync(string pastedText)
    {
        if (string.IsNullOrWhiteSpace(pastedText))
        {
            HasAcknowledgedTransactionImportErrors = false;
            TransactionImportSummary = null;
            ClearTransactionImportPreview();
            return;
        }

        try
        {
            var preview = await _transactionImportService.PreviewImportAsync(pastedText);

            TransactionImportErrorRows.Clear();
            foreach (var error in preview.ErrorRows)
            {
                TransactionImportErrorRows.Add(error);
            }

            NewTransactionImports.Clear();
            foreach (var transaction in preview.NewTransactions)
            {
                NewTransactionImports.Add(transaction);
            }

            foreach (var duplicateTransaction in DuplicateTransactionImports)
            {
                duplicateTransaction.PropertyChanged -= OnDuplicateTransactionImportPropertyChanged;
            }

            DuplicateTransactionImports.Clear();
            foreach (var duplicateTransaction in preview.DuplicateTransactions)
            {
                var duplicateItem = new DuplicateTransactionImportItem(duplicateTransaction);
                duplicateItem.PropertyChanged += OnDuplicateTransactionImportPropertyChanged;
                DuplicateTransactionImports.Add(duplicateItem);
            }

            if (preview.ErrorRows.Count > 0)
            {
                HasAcknowledgedTransactionImportErrors = false;
            }

            TransactionImportSummary =
                $"{preview.NewTransactions.Count} new, {preview.DuplicateTransactions.Count} duplicate, {preview.ErrorRows.Count} error row(s).";
            OnPropertyChanged(nameof(HasTransactionImportErrors));
            OnPropertyChanged(nameof(CanImportTransactions));
            ImportTransactionsCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error previewing transactions: {ex.Message}";
        }
    }

    private void ClearTransactionImportPreview()
    {
        foreach (var duplicateTransaction in DuplicateTransactionImports)
        {
            duplicateTransaction.PropertyChanged -= OnDuplicateTransactionImportPropertyChanged;
        }

        TransactionImportErrorRows.Clear();
        NewTransactionImports.Clear();
        DuplicateTransactionImports.Clear();
        OnPropertyChanged(nameof(HasTransactionImportErrors));
        OnPropertyChanged(nameof(CanImportTransactions));
        ImportTransactionsCommand.NotifyCanExecuteChanged();
    }

    private void OnDuplicateTransactionImportPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DuplicateTransactionImportItem.SelectedDecisionOption) or
            nameof(DuplicateTransactionImportItem.HasDecision) or
            nameof(DuplicateTransactionImportItem.KeepTransaction))
        {
            OnPropertyChanged(nameof(CanImportTransactions));
            ImportTransactionsCommand.NotifyCanExecuteChanged();
        }
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
        return docs.Where(d =>
            d.IsFutureTransaction ||
            d.IsAnnual ||
            IsInMonthRange(d.FileNameDate, selectedYear, selectedMonth));
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

    private DateOnly GetEffectiveSourceDocumentMonth() =>
        UseSeparateSourceDocumentMonth
            ? new DateOnly(SelectedSourceDocumentYear, SelectedSourceDocumentMonth, 1)
            : new DateOnly(SelectedYear, SelectedMonth, 1);

    private static int GetSourceDocumentSortRank(SourceDocument doc) =>
        doc.IsFutureTransaction
            ? 0
            : doc.IsAnnual || doc.IsExpiredAnnual
                ? 1
                : 2;

    private void SetSourceDocumentMonth(DateOnly month)
    {
        if (GetEffectiveSourceDocumentMonth() == month)
            return;

        ApplySourceDocumentMonth(month);
        _ = RefreshAsync();
    }

    private void ApplySourceDocumentMonth(DateOnly month)
    {
        SelectedSourceDocumentYear = month.Year;
        SelectedSourceDocumentMonth = month.Month;
        var matchesTransactionMonth = month.Year == SelectedYear && month.Month == SelectedMonth;
        if (!matchesTransactionMonth)
        {
            UseSeparateSourceDocumentMonth = true;
        }
    }
}

public enum ViewScope
{
    CurrentMonth,
    AdjacentMonths,
    AllMonths,
}

public enum FilterMode
{
    SeeAll,
    SeeMonth,
}

public sealed record LinkedPair(Transaction Transaction, SourceDocument SourceDocument);

public enum DuplicateTransactionImportDecision
{
    Keep,
    Reject,
}

public sealed record DuplicateTransactionImportDecisionOption(
    DuplicateTransactionImportDecision Decision,
    string Label);

public partial class DuplicateTransactionImportItem : ObservableObject
{
    private static readonly IReadOnlyList<DuplicateTransactionImportDecisionOption> AvailableDecisionOptions =
    [
        new(DuplicateTransactionImportDecision.Keep, "Keep transaction"),
        new(DuplicateTransactionImportDecision.Reject, "Reject transaction"),
    ];

    private readonly TransactionImportPreviewTransaction _previewTransaction;

    public DuplicateTransactionImportItem(TransactionImportPreviewTransaction previewTransaction)
    {
        _previewTransaction = previewTransaction;
    }

    public int LineNumber => _previewTransaction.LineNumber;
    public DateOnly AccountingDate => _previewTransaction.AccountingDate;
    public DateOnly TransactionDate => _previewTransaction.TransactionDate;
    public TransactionType TransactionType => _previewTransaction.TransactionType;
    public string TransactionTypeDisplay => _previewTransaction.TransactionTypeDisplay;
    public string Description => _previewTransaction.Description;
    public decimal Amount => _previewTransaction.Amount;
    public string? DuplicateReason => _previewTransaction.DuplicateReason;
    public IReadOnlyList<DuplicateTransactionImportDecisionOption> DecisionOptions => AvailableDecisionOptions;
    public bool HasDecision => SelectedDecisionOption is not null;
    public bool KeepTransaction => SelectedDecisionOption?.Decision == DuplicateTransactionImportDecision.Keep;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDecision))]
    [NotifyPropertyChangedFor(nameof(KeepTransaction))]
    public partial DuplicateTransactionImportDecisionOption? SelectedDecisionOption { get; set; }

    public TransactionImportPreviewTransaction ToPreviewTransaction() => _previewTransaction;
}

public sealed record LinkedTransactionGroup(
    Transaction Transaction,
    IReadOnlyList<LinkedPair> LinkedDocuments);

public sealed record MonthOption(DateOnly Date)
{
    public override string ToString() => Date.ToString("MMMM yyyy");
}
