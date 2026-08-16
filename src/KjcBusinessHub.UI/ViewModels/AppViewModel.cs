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
    private readonly SourceDocumentValidator _sourceDocumentValidator = new();

    public ObservableCollection<Transaction> UnlinkedTransactions { get; } = [];
    public ObservableCollection<SourceDocument> AvailableSourceDocuments { get; } = [];
    public ObservableCollection<LinkedPair> LinkedPairs { get; } = [];
    public IReadOnlyList<SourceDocumentCurrency> SupportedCurrencies { get; } =
        Enum.GetValues<SourceDocumentCurrency>();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkDocumentCommand))]
    public partial Transaction? SelectedUnlinkedTransaction { get; set; }

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
    public partial string CcyAmountInputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SourceDocumentCurrency? SelectedCurrency { get; set; }

    public bool IsSettingAmount => DocumentBeingAmounted is not null;

    // --- Filter state ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSeeMonthMode))]
    [NotifyPropertyChangedFor(nameof(IsSeeAllMode))]
    public partial FilterMode FilterMode { get; set; } = FilterMode.SeeAll;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    public partial int SelectedYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    public partial int SelectedMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today).Month;

    [ObservableProperty]
    public partial bool IncludeNeighbouringMonths { get; set; } = false;

    public bool IsSeeAllMode => FilterMode == FilterMode.SeeAll;
    public bool IsSeeMonthMode => FilterMode == FilterMode.SeeMonth;

    public string SelectedMonthLabel =>
        new DateOnly(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");

    public AppViewModel(
        ITransactionRepository transactionRepository,
        ISourceDocumentRepository sourceDocumentRepository,
        TransactionImportService transactionImportService,
        SourceDocumentImportService sourceDocumentImportService,
        FileWatcherService fileWatcherService,
        ISettingsService settings,
        IFileSystemService fileSystemService)
    {
        _transactionRepository = transactionRepository;
        _sourceDocumentRepository = sourceDocumentRepository;
        _transactionImportService = transactionImportService;
        _sourceDocumentImportService = sourceDocumentImportService;
        _fileWatcherService = fileWatcherService;
        _settings = settings;
        _fileSystemService = fileSystemService;
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

    partial void OnIncludeNeighbouringMonthsChanged(bool value) =>
        _ = RefreshAsync();

    // --- Linking commands ---

    private bool CanLinkDocument() =>
        SelectedUnlinkedTransaction is not null &&
        SelectedAvailableSourceDocument is not null &&
        SelectedAvailableSourceDocument.Status == SourceDocumentStatus.Active;

    [RelayCommand(CanExecute = nameof(CanLinkDocument))]
    private async Task LinkDocumentAsync()
    {
        if (SelectedUnlinkedTransaction is null || SelectedAvailableSourceDocument is null)
            return;

        try
        {
            await _transactionRepository.LinkDocumentAsync(
                SelectedUnlinkedTransaction.Id,
                SelectedAvailableSourceDocument.Id);
            await _transactionRepository.SaveChangesAsync();
            SelectedUnlinkedTransaction = null;
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
            var previousAmount = DocumentBeingAmounted.Amount;
            var previousCcyAmount = DocumentBeingAmounted.CcyAmount;
            var previousCurrency = DocumentBeingAmounted.Ccy;

            DocumentBeingAmounted.Amount = amount;
            DocumentBeingAmounted.CcyAmount = ccyAmount;
            DocumentBeingAmounted.Ccy = selectedCurrency;

            var validationResult = _sourceDocumentValidator.ValidateSetAmount(DocumentBeingAmounted);
            if (!validationResult.IsValid)
            {
                DocumentBeingAmounted.Amount = previousAmount;
                DocumentBeingAmounted.CcyAmount = previousCcyAmount;
                DocumentBeingAmounted.Ccy = previousCurrency;
                StatusMessage = string.Join(" ", validationResult.Errors.Select(error => error.Message));
                return;
            }

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

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var allTransactions = await _transactionRepository.GetAllAsync();
        var allDocs = await _sourceDocumentRepository.GetAllAsync();

        UnlinkedTransactions.Clear();
        LinkedPairs.Clear();
        AvailableSourceDocuments.Clear();

        // Linked pairs are intentionally not subject to the month filter: they represent
        // confirmed matches and should remain visible for context even when browsing a
        // specific month.
        var activeTransactions = allTransactions
            .Where(t => t.Status == TransactionStatus.Active)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var linkedTransactions = activeTransactions.Where(t => t.SourceDocuments.Count > 0).ToList();
        var unlinkedTransactions = activeTransactions.Where(t => t.SourceDocuments.Count == 0).ToList();

        // Linked pairs sorted by transaction date, then document date
        foreach (var tx in linkedTransactions)
        {
            foreach (var doc in tx.SourceDocuments.OrderBy(d => d.FileNameDate))
            {
                LinkedPairs.Add(new LinkedPair(tx, doc));
            }
        }

        // Unlinked transactions — apply optional month filter
        var filteredUnlinked = ApplyTransactionMonthFilter(unlinkedTransactions);
        foreach (var tx in filteredUnlinked)
        {
            UnlinkedTransactions.Add(tx);
        }

        // Available source documents — apply optional month filter
        var visibleDocs = allDocs
            .Where(d =>
                d.Status != SourceDocumentStatus.Removed &&
                d.Status != SourceDocumentStatus.RemovedFromDisk)
            .OrderBy(d => d.FileNameDate)
            .ToList();

        var filteredDocs = ApplyDocumentMonthFilter(visibleDocs);
        foreach (var doc in filteredDocs)
        {
            AvailableSourceDocuments.Add(doc);
        }
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

        return transactions.Where(t => IsInMonthRange(t.TransactionDate));
    }

    private IEnumerable<SourceDocument> ApplyDocumentMonthFilter(IEnumerable<SourceDocument> docs)
    {
        if (FilterMode == FilterMode.SeeAll)
            return docs;

        return docs.Where(d => IsInMonthRange(d.FileNameDate));
    }

    private bool IsInMonthRange(DateOnly date)
    {
        var selected = new DateOnly(SelectedYear, SelectedMonth, 1);

        if (IncludeNeighbouringMonths)
        {
            var prev = selected.AddMonths(-1);
            var next = selected.AddMonths(1);
            return (date.Year == prev.Year && date.Month == prev.Month) ||
                   (date.Year == selected.Year && date.Month == selected.Month) ||
                   (date.Year == next.Year && date.Month == next.Month);
        }

        return date.Year == SelectedYear && date.Month == SelectedMonth;
    }
}

public enum FilterMode
{
    SeeAll,
    SeeMonth,
}

public sealed record LinkedPair(Transaction Transaction, SourceDocument SourceDocument);
