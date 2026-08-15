using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Application.Services;

/// <summary>
/// Watches for changes to the Transactions file and the SourceDocuments folder,
/// triggering re-import on changes (UC-0103, UC-0104).
/// </summary>
public sealed class FileWatcherService : IDisposable
{
    private readonly TransactionImportService _transactionImportService;
    private readonly SourceDocumentImportService _sourceDocumentImportService;
    private readonly ISettingsService _settings;
    private readonly ILogger<FileWatcherService> _logger;

    private FileSystemWatcher? _transactionWatcher;
    private FileSystemWatcher? _sourceDocumentWatcher;
    private readonly SemaphoreSlim _transactionLock = new(1, 1);
    private readonly SemaphoreSlim _sourceDocumentLock = new(1, 1);
    private bool _disposed;

    public FileWatcherService(
        TransactionImportService transactionImportService,
        SourceDocumentImportService sourceDocumentImportService,
        ISettingsService settings,
        ILogger<FileWatcherService> logger)
    {
        _transactionImportService = transactionImportService;
        _sourceDocumentImportService = sourceDocumentImportService;
        _settings = settings;
        _logger = logger;
    }

    public void Start()
    {
        if (!_settings.IsConfigured || _settings.SourceDocumentFolder is null)
        {
            _logger.LogError("FileWatcherService cannot start: SourceDocumentFolder is not configured.");
            return;
        }

        var folder = _settings.SourceDocumentFolder;

        // Watch the Transactions file
        _transactionWatcher = new FileSystemWatcher(folder, TransactionImportService.TransactionsFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _transactionWatcher.Changed += OnTransactionFileChanged;
        _transactionWatcher.Created += OnTransactionFileChanged;
        _logger.LogInformation("Watching Transactions file in {Folder}.", folder);

        // Watch the SourceDocuments folder recursively
        _sourceDocumentWatcher = new FileSystemWatcher(folder)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };
        _sourceDocumentWatcher.Changed += OnSourceDocumentFolderChanged;
        _sourceDocumentWatcher.Created += OnSourceDocumentFolderChanged;
        _sourceDocumentWatcher.Deleted += OnSourceDocumentFolderChanged;
        _sourceDocumentWatcher.Renamed += OnSourceDocumentFolderChanged;
        _logger.LogInformation("Watching SourceDocuments folder {Folder}.", folder);
    }

    public void Stop()
    {
        if (_transactionWatcher is not null)
        {
            _transactionWatcher.EnableRaisingEvents = false;
            _transactionWatcher.Dispose();
            _transactionWatcher = null;
        }
        if (_sourceDocumentWatcher is not null)
        {
            _sourceDocumentWatcher.EnableRaisingEvents = false;
            _sourceDocumentWatcher.Dispose();
            _sourceDocumentWatcher = null;
        }
    }

    private void OnTransactionFileChanged(object sender, FileSystemEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            if (!await _transactionLock.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                _logger.LogError("Timed out waiting for transaction import lock; skipping re-import triggered by '{FullPath}'.", e.FullPath);
                return;
            }
            try
            {
                _logger.LogInformation("Transactions file changed, re-importing.");
                await _transactionImportService.ImportAsync(_settings.SourceDocumentFolder!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-importing Transactions file.");
            }
            finally
            {
                _transactionLock.Release();
            }
        });
    }

    private void OnSourceDocumentFolderChanged(object sender, FileSystemEventArgs e)
    {
        // Ignore changes to the Transactions file itself
        if (string.Equals(Path.GetFileName(e.FullPath), TransactionImportService.TransactionsFileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            if (!await _sourceDocumentLock.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                _logger.LogError("Timed out waiting for source document import lock; skipping re-import triggered by '{FullPath}'.", e.FullPath);
                return;
            }
            try
            {
                _logger.LogDebug("SourceDocuments folder changed, re-importing.");
                await _sourceDocumentImportService.ImportAsync(_settings.SourceDocumentFolder!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-importing SourceDocuments.");
            }
            finally
            {
                _sourceDocumentLock.Release();
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        Stop();
        _transactionLock.Dispose();
        _sourceDocumentLock.Dispose();
    }
}
