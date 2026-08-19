namespace KjcBusinessHub.Application.Interfaces;

public interface ISettingsService
{
    string? SourceDocumentFolder { get; set; }
    bool CloseToSystemTray { get; set; }
    bool IsConfigured { get; }
    void Save();
}
