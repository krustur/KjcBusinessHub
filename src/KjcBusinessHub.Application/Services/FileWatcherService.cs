using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Application.Services;

/// <summary>
/// Watches for changes to the SourceDocuments folder,
/// triggering re-import on changes (UC-0104).
/// </summary>
public sealed class FileWatcherService : IDisposable
{
    private readonly SourceDocumentImportService _sourceDocumentImportService;
    private readonly ISettingsService _settings;
    private readonly ILogger<FileWatcherService> _logger;

    private FileSystemWatcher? _sourceDocumentWatcher;
    private readonly SemaphoreSlim _sourceDocumentLock = new(1, 1);
    private bool _disposed;

    public FileWatcherService(
        SourceDocumentImportService sourceDocumentImportService,
        ISettingsService settings,
        ILogger<FileWatcherService> logger)
    {
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
        if (_sourceDocumentWatcher is not null)
        {
            _sourceDocumentWatcher.EnableRaisingEvents = false;
            _sourceDocumentWatcher.Dispose();
            _sourceDocumentWatcher = null;
        }
    }

    private void OnSourceDocumentFolderChanged(object sender, FileSystemEventArgs e)
    {
        if (string.Equals(Path.GetFileName(e.FullPath), "Consulting-Transactions.txt", StringComparison.OrdinalIgnoreCase))
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
        _sourceDocumentLock.Dispose();
    }
}
