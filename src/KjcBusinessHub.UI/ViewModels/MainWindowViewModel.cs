using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly AppViewModel _appViewModel;
    private readonly CalendarViewModel _calendarViewModel;

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; }

    public MainWindowViewModel(
        ISettingsService settings,
        SettingsViewModel settingsVm,
        AppViewModel appVm,
        CalendarViewModel calendarVm)
    {
        _settings = settings;
        _settingsViewModel = settingsVm;
        _appViewModel = appVm;
        _calendarViewModel = calendarVm;

        settingsVm.NavigateToApp = ShowApp;
        appVm.NavigateToCalendar = ShowCalendar;
        calendarVm.NavigateToApp = ShowApp;

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

    public void ShowCalendar()
    {
        CurrentPage = _calendarViewModel;
        _ = _calendarViewModel.LoadAsync();
    }
}
