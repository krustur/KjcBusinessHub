using System.Text.Json;
using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Infrastructure.Settings;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;
    private AppSettings _settings;

    public SettingsService(string settingsFilePath, ILogger<SettingsService> logger)
    {
        _settingsFilePath = settingsFilePath;
        _logger = logger;
        _settings = Load();
    }

    public string? SourceDocumentFolder
    {
        get => _settings.SourceDocumentFolder;
        set
        {
            _settings.SourceDocumentFolder = value;
        }
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.SourceDocumentFolder) &&
        Directory.Exists(_settings.SourceDocumentFolder);

    public bool CloseToSystemTray
    {
        get => _settings.CloseToSystemTray;
        set => _settings.CloseToSystemTray = value;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    private AppSettings Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read settings from '{SettingsFilePath}'. Fix or delete the file and restart the application.", _settingsFilePath);
            throw;
        }
    }

    private sealed class AppSettings
    {
        public string? SourceDocumentFolder { get; set; }
        public bool CloseToSystemTray { get; set; }
    }
}
