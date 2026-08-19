using CommunityToolkit.Mvvm.ComponentModel;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly AppViewModel _appViewModel;

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; }

    public MainWindowViewModel(ISettingsService settings, SettingsViewModel settingsVm, AppViewModel appVm)
    {
        _settings = settings;
        _settingsViewModel = settingsVm;
        _appViewModel = appVm;

        settingsVm.NavigateToApp = ShowApp;

        if (_settings.IsConfigured)
            CurrentPage = _appViewModel;
        else
            CurrentPage = _settingsViewModel;
    }

    public void ShowSettings()
    {
        CurrentPage = _settingsViewModel;
    }

    public void ShowApp()
    {
        CurrentPage = _appViewModel;
    }
}
