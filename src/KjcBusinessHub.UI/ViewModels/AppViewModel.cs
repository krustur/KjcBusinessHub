using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;

namespace KjcBusinessHub.UI.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISourceDocumentRepository _sourceDocumentRepository;
    private readonly TransactionImportService _transactionImportService;
    private readonly SourceDocumentImportService _sourceDocumentImportService;
    private readonly FileWatcherService _fileWatcherService;
    private readonly ISettingsService _settings;

    public ObservableCollection<Transaction> UnlinkedTransactions { get; } = [];
    public ObservableCollection<SourceDocument> UnlinkedSourceDocuments { get; } = [];
    public ObservableCollection<LinkedPair> LinkedPairs { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public AppViewModel(
        ITransactionRepository transactionRepository,
        ISourceDocumentRepository sourceDocumentRepository,
        TransactionImportService transactionImportService,
        SourceDocumentImportService sourceDocumentImportService,
        FileWatcherService fileWatcherService,
        ISettingsService settings)
    {
        _transactionRepository = transactionRepository;
        _sourceDocumentRepository = sourceDocumentRepository;
        _transactionImportService = transactionImportService;
        _sourceDocumentImportService = sourceDocumentImportService;
        _fileWatcherService = fileWatcherService;
        _settings = settings;
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

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var allTransactions = await _transactionRepository.GetAllAsync();
        var allDocs = await _sourceDocumentRepository.GetAllAsync();

        UnlinkedTransactions.Clear();
        LinkedPairs.Clear();
        UnlinkedSourceDocuments.Clear();

        var linkedDocIds = new HashSet<Guid>();

        foreach (var tx in allTransactions.Where(t => t.Status == TransactionStatus.Active))
        {
            if (tx.SourceDocuments.Count > 0)
            {
                foreach (var doc in tx.SourceDocuments)
                {
                    LinkedPairs.Add(new LinkedPair(tx, doc));
                    linkedDocIds.Add(doc.Id);
                }
            }
            else
            {
                UnlinkedTransactions.Add(tx);
            }
        }

        foreach (var doc in allDocs.Where(d =>
            d.Status != SourceDocumentStatus.Removed &&
            d.Status != SourceDocumentStatus.RemovedFromDisk &&
            !linkedDocIds.Contains(d.Id)))
        {
            UnlinkedSourceDocuments.Add(doc);
        }
    }
}

public sealed record LinkedPair(Transaction Transaction, SourceDocument SourceDocument);
