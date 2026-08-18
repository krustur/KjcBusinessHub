using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly AppViewModel _appViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPageTitle))]
    [NotifyPropertyChangedFor(nameof(CurrentPageSubtitle))]
    [NotifyPropertyChangedFor(nameof(IsShowingMatching))]
    [NotifyPropertyChangedFor(nameof(IsShowingSettings))]
    public partial AppSection CurrentSection { get; set; }

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; }

    public bool IsMatchingAvailable => _settings.IsConfigured;
    public bool IsShowingMatching => CurrentSection == AppSection.Matching;
    public bool IsShowingSettings => CurrentSection == AppSection.Settings;

    public string CurrentPageTitle => CurrentSection switch
    {
        AppSection.Settings => "Settings",
        _ => "Matching workspace",
    };

    public string CurrentPageSubtitle => CurrentSection switch
    {
        AppSection.Settings => "Configure the document folder and review setup guidance.",
        _ => "Match transactions, review document status, and track monthly completion from one place.",
    };

    public MainWindowViewModel(ISettingsService settings, SettingsViewModel settingsVm, AppViewModel appVm)
    {
        _settings = settings;
        _settingsViewModel = settingsVm;
        _appViewModel = appVm;

        settingsVm.NavigateToApp = () =>
        {
            OnPropertyChanged(nameof(IsMatchingAvailable));
            ShowMatching();
        };

        if (_settings.IsConfigured)
        {
            CurrentSection = AppSection.Matching;
            CurrentPage = _appViewModel;
        }
        else
        {
            CurrentSection = AppSection.Settings;
            CurrentPage = _settingsViewModel;
        }
    }

    [RelayCommand]
    private void ShowMatching()
    {
        if (!_settings.IsConfigured)
            return;

        CurrentSection = AppSection.Matching;
        CurrentPage = _appViewModel;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        CurrentSection = AppSection.Settings;
        CurrentPage = _settingsViewModel;
    }
}

public enum AppSection
{
    Matching,
    Settings,
}
