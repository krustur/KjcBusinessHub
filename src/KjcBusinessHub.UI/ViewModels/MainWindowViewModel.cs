using CommunityToolkit.Mvvm.ComponentModel;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; }

    public MainWindowViewModel(ISettingsService settings, SettingsViewModel settingsVm, AppViewModel appVm)
    {
        _settings = settings;

        settingsVm.NavigateToApp = () => CurrentPage = appVm;

        if (_settings.IsConfigured)
            CurrentPage = appVm;
        else
            CurrentPage = settingsVm;
    }
}
