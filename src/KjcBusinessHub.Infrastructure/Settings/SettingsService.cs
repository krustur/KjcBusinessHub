using System.Text.Json;
using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Infrastructure.Settings;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KjcBusinessHub",
        "settings.json");

    private readonly ILogger<SettingsService> _logger;
    private AppSettings _settings;

    public SettingsService(ILogger<SettingsService> logger)
    {
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

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsFilePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFilePath, json);
    }

    private AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read settings from '{SettingsFilePath}'. Fix or delete the file and restart the application.", SettingsFilePath);
            throw;
        }
    }

    private sealed class AppSettings
    {
        public string? SourceDocumentFolder { get; set; }
    }
}
