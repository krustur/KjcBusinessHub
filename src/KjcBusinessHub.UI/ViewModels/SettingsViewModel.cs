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
    [NotifyPropertyChangedFor(nameof(HasFeedbackMessage))]
    public partial string? FeedbackMessage { get; set; }

    [ObservableProperty]
    public partial StatusTone FeedbackTone { get; set; } = StatusTone.Info;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConfigurationStatusTitle))]
    [NotifyPropertyChangedFor(nameof(ConfigurationStatusDescription))]
    public partial bool CanNavigate { get; set; }

    public Action? NavigateToApp { get; set; }
    public bool HasFeedbackMessage => !string.IsNullOrWhiteSpace(FeedbackMessage);

    public string ConfigurationStatusTitle => CanNavigate ? "Workspace ready" : "Setup required";

    public string ConfigurationStatusDescription => CanNavigate
        ? "The document folder is configured. You can open the matching workspace at any time."
        : "Choose the folder that contains the transactions file and the source documents.";

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        SourceDocumentFolder = settings.SourceDocumentFolder ?? string.Empty;
        CanNavigate = settings.IsConfigured;
    }

    [RelayCommand]
    private void Save()
    {
        FeedbackMessage = null;

        if (string.IsNullOrWhiteSpace(SourceDocumentFolder))
        {
            FeedbackTone = StatusTone.Error;
            FeedbackMessage = "Source document folder is required.";
            return;
        }

        if (!Directory.Exists(SourceDocumentFolder))
        {
            FeedbackTone = StatusTone.Error;
            FeedbackMessage = "The specified folder does not exist.";
            return;
        }

        _settings.SourceDocumentFolder = SourceDocumentFolder;
        _settings.Save();
        CanNavigate = true;
        FeedbackTone = StatusTone.Success;
        FeedbackMessage = "Folder saved. You can continue to the matching workspace.";
    }

    [RelayCommand]
    private void GoToApp()
    {
        if (!CanNavigate) return;
        NavigateToApp?.Invoke();
    }
}
