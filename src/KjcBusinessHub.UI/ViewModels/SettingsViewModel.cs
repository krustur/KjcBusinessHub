using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty]
    public partial string SourceDocumentFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool CanNavigate { get; set; }

    [ObservableProperty]
    public partial bool CloseToSystemTray { get; set; }

    public Action? NavigateToApp { get; set; }

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        SourceDocumentFolder = settings.SourceDocumentFolder ?? string.Empty;
        CloseToSystemTray = settings.CloseToSystemTray;
        CanNavigate = settings.IsConfigured;
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(SourceDocumentFolder))
        {
            ErrorMessage = "SourceDocumentFolder is required.";
            return;
        }

        if (!Directory.Exists(SourceDocumentFolder))
        {
            ErrorMessage = "The specified folder does not exist.";
            return;
        }

        _settings.SourceDocumentFolder = SourceDocumentFolder;
        _settings.CloseToSystemTray = CloseToSystemTray;
        _settings.Save();
        CanNavigate = true;
    }

    [RelayCommand]
    private void GoToApp()
    {
        if (!CanNavigate) return;
        NavigateToApp?.Invoke();
    }
}
