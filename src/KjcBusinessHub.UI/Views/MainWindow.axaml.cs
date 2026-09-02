using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.UI.ViewModels;

namespace KjcBusinessHub.UI.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;
    private ISettingsService? _settings;
    private UpdateService? _updateService;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _closeToTrayMenuItem;
    private bool _isQuitting;
    private bool _isTogglingCloseToTray;
    private bool _startupUpdateFlowStarted;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    public void Configure(MainWindowViewModel viewModel, ISettingsService settings, UpdateService updateService)
    {
        _viewModel = viewModel;
        _settings = settings;
        _updateService = updateService;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        EnsureTrayIcon();
        if (_startupUpdateFlowStarted)
        {
            return;
        }

        _startupUpdateFlowStarted = true;
        _ = RunStartupUpdateFlowAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isQuitting || _settings is null)
        {
            return;
        }

        e.Cancel = true;

        if (WindowClosePolicy.Decide(_settings.CloseToSystemTray) == WindowCloseDecision.HideToTray)
        {
            HideToTray();
            return;
        }

        _ = RequestQuitAsync();
    }

    private async Task RequestQuitAsync()
    {
        if (!await ShowQuitConfirmationAsync())
        {
            return;
        }

        _isQuitting = true;
        Close();
    }

    private async Task<bool> ShowQuitConfirmationAsync()
    {
        var confirmWindow = new Window
        {
            Width = 360,
            Height = 150,
            CanResize = false,
            Title = "Confirm quit",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = BuildConfirmationDialogContent(),
        };

        if (Icon is not null)
        {
            confirmWindow.Icon = Icon;
        }

        var closeResult = false;
        if (confirmWindow.Content is StackPanel panel)
        {
            var yesButton = (Button)((StackPanel)panel.Children[1]).Children[0]!;
            var noButton = (Button)((StackPanel)panel.Children[1]).Children[1]!;

            yesButton.Click += (_, _) =>
            {
                closeResult = true;
                confirmWindow.Close();
            };
            noButton.Click += (_, _) => confirmWindow.Close();
        }

        await confirmWindow.ShowDialog(this);
        return closeResult;
    }

    private static StackPanel BuildConfirmationDialogContent() =>
        new()
        {
            Spacing = 18,
            Margin = new Thickness(18),
            Children =
            {
                new TextBlock
                {
                    Text = "Are you sure you want to quit KJC Business Hub?",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children =
                    {
                        new Button { Content = "Yes", MinWidth = 80, Classes = { "accent" } },
                        new Button { Content = "No", MinWidth = 80 },
                    }
                }
            }
        };

    private void EnsureTrayIcon()
    {
        if (_trayIcon is not null || _settings is null)
        {
            return;
        }

        var icon = new WindowIcon(
            AssetLoader.Open(new Uri("avares://KjcBusinessHub.UI/Assets/kjcbusinesshub-tray.ico")));

        _closeToTrayMenuItem = new NativeMenuItem("Close to system tray")
        {
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = _settings.CloseToSystemTray,
        };
        _closeToTrayMenuItem.Click += OnToggleCloseToTrayClicked;

        var checkForUpdatesItem = new NativeMenuItem("Check for Updates");
        checkForUpdatesItem.Click += async (_, _) => await CheckForUpdatesAsync(UpdateChannel.Stable);

        var checkForPrereleaseUpdatesItem = new NativeMenuItem("Check for Updates (pre-release)");
        checkForPrereleaseUpdatesItem.Click += async (_, _) => await CheckForUpdatesAsync(UpdateChannel.Prerelease);

        var aboutItem = new NativeMenuItem("About");
        aboutItem.Click += async (_, _) => await ShowAboutDialogAsync();

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += async (_, _) => await RequestQuitAsync();

        var menu = new NativeMenu
        {
            Items =
            {
                _closeToTrayMenuItem,
                new NativeMenuItemSeparator(),
                checkForUpdatesItem,
                checkForPrereleaseUpdatesItem,
                aboutItem,
                new NativeMenuItemSeparator(),
                quitItem,
            }
        };

        _trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "KJC Business Hub",
            IsVisible = true,
            Menu = menu,
        };
        _trayIcon.Clicked += (_, _) => ToggleMainWindow();

        if (Avalonia.Application.Current is not null)
        {
            TrayIcon.SetIcons(Avalonia.Application.Current, new TrayIcons { _trayIcon });
        }
    }

    private void ToggleMainWindow()
    {
        if (!IsVisible || WindowState == WindowState.Minimized)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            return;
        }

        HideToTray();
    }

    private void HideToTray()
    {
        WindowState = WindowState.Minimized;
        Hide();
    }

    private void OnToggleCloseToTrayClicked(object? sender, EventArgs e)
    {
        if (_settings is null || _closeToTrayMenuItem is null || _isTogglingCloseToTray)
        {
            return;
        }

        _isTogglingCloseToTray = true;
        try
        {
            _settings.CloseToSystemTray = _closeToTrayMenuItem.IsChecked;
            _settings.Save();
        }
        finally
        {
            _isTogglingCloseToTray = false;
        }
    }

    private async Task ShowAboutDialogAsync()
    {
        var aboutWindow = new Window
        {
            Width = 650,
            Height = 340,
            CanResize = false,
            Title = "About KJC Business Hub",
            WindowStartupLocation = IsVisible
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen,
        };

        aboutWindow.Content = BuildAboutDialogContent(aboutWindow);

        if (Icon is not null)
        {
            aboutWindow.Icon = Icon;
        }

        if (IsVisible)
        {
            await aboutWindow.ShowDialog(this);
            return;
        }

        aboutWindow.Show();
    }

    public Task ShowAboutDialogFromUiAsync() => ShowAboutDialogAsync();

    private async Task CheckForUpdatesAsync(UpdateChannel channel, Window? owner = null)
    {
        if (_updateService is null)
        {
            return;
        }

        var result = await _updateService.CheckAndApplyUpdatesAsync(channel);
        if (result.Status == UpdateCheckStatus.UpdateApplied)
        {
            return;
        }

        await ShowInformationDialogAsync(
            channel == UpdateChannel.Prerelease ? "Check for Updates (pre-release)" : "Check for Updates",
            result.Message,
            owner);
    }

    private async Task RunStartupUpdateFlowAsync()
    {
        if (_updateService is null)
        {
            return;
        }

        var pendingFailure = _updateService.ConsumePendingFailureNotification();
        if (!string.IsNullOrWhiteSpace(pendingFailure))
        {
            await ShowInformationDialogAsync("Update failed", pendingFailure, this);
        }

        var result = await _updateService.CheckAndApplyUpdatesInBackgroundAsync();
        if (result is { Status: UpdateCheckStatus.Failed })
        {
            await ShowInformationDialogAsync("Update failed", result.Message, this);
        }
    }

    private async Task ShowInformationDialogAsync(string title, string message, Window? owner = null)
    {
        var infoWindow = new Window
        {
            Width = 420,
            Height = 180,
            CanResize = false,
            Title = title,
            WindowStartupLocation = owner is not null && owner.IsVisible
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen,
            Content = BuildInformationDialogContent(message),
        };

        if (Icon is not null)
        {
            infoWindow.Icon = Icon;
        }

        if (infoWindow.Content is StackPanel panel &&
            panel.Children.OfType<Button>().FirstOrDefault() is { } closeButton)
        {
            closeButton.Click += (_, _) => infoWindow.Close();
        }

        if (owner is not null && owner.IsVisible)
        {
            await infoWindow.ShowDialog(owner);
            return;
        }

        infoWindow.Show();
    }

    private static StackPanel BuildInformationDialogContent(string message) =>
        new()
        {
            Spacing = 18,
            Margin = new Thickness(18),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                },
                new Button
                {
                    Content = "Close",
                    Width = 90,
                    HorizontalAlignment = HorizontalAlignment.Right,
                },
            }
        };

    private Panel BuildAboutDialogContent(Window aboutWindow)
    {
        var appFilePath = ResolveAppFilePath();
        var runtimeProfile = Program.RuntimeProfile;
        var appVersion = "N/A";
        var fileVersion = "N/A";
        var productVersion = "N/A";
        var copyright = "N/A";
        var buildDateTimeText = "N/A";

        if (!string.IsNullOrWhiteSpace(appFilePath) && File.Exists(appFilePath))
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(appFilePath);
                if (!string.IsNullOrWhiteSpace(versionInfo.FileVersion))
                {
                    fileVersion = versionInfo.FileVersion;
                }

                if (!string.IsNullOrWhiteSpace(versionInfo.ProductVersion))
                {
                    productVersion = versionInfo.ProductVersion;
                }

                if (!string.IsNullOrWhiteSpace(versionInfo.LegalCopyright))
                {
                    copyright = versionInfo.LegalCopyright;
                }

                var buildDateTime = File.GetLastWriteTime(appFilePath);
                buildDateTimeText = buildDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                // Keep N/A fallbacks if metadata cannot be read.
            }
        }

        var informationalVersion =
            typeof(MainWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            appVersion = informationalVersion;
        }
        else
        {
            appVersion = typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "N/A";
        }

        var details = string.Join(Environment.NewLine, new[]
        {
            $"Application Version: {appVersion}",
            $"File Version: {fileVersion}",
            $"Product Version: {productVersion}",
            $"Copyright: {copyright}",
            $"Build Date/Time: {buildDateTimeText}",
            $"Application Path: {appFilePath}",
            $"Database Path: {runtimeProfile.DatabasePath}",
            $"Config Path: {runtimeProfile.SettingsPath}",
        });

        var checkForUpdatesButton = new Button
        {
            Content = "Check for Updates",
            MinWidth = 170,
        };
        checkForUpdatesButton.Click += async (_, _) => await CheckForUpdatesAsync(UpdateChannel.Stable, aboutWindow);

        var checkForPrereleaseUpdatesButton = new Button
        {
            Content = "Check for Updates (pre-release)",
            MinWidth = 220,
        };
        checkForPrereleaseUpdatesButton.Click += async (_, _) => await CheckForUpdatesAsync(UpdateChannel.Prerelease, aboutWindow);

        var closeButton = new Button
        {
            Content = "Close",
            Width = 90,
        };
        closeButton.Click += (_, _) => aboutWindow.Close();

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children =
            {
                checkForUpdatesButton,
                checkForPrereleaseUpdatesButton,
                closeButton,
            }
        };
        DockPanel.SetDock(actionRow, Dock.Bottom);

        return new DockPanel
        {
            Margin = new Thickness(18),
            LastChildFill = true,
            Children =
            {
                actionRow,
                new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "KJC Business Hub",
                            FontSize = 20,
                            FontWeight = FontWeight.SemiBold,
                        },
                        new TextBlock
                        {
                            Text = details,
                            TextWrapping = TextWrapping.Wrap,
                        }
                    }
                },
            }
        };
    }

    private static string ResolveAppFilePath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
        {
            return Environment.ProcessPath;
        }

        var assemblyPath = typeof(MainWindow).Assembly.Location;
        if (!string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
        {
            return assemblyPath;
        }

        var entryAssemblyPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(entryAssemblyPath) && File.Exists(entryAssemblyPath))
        {
            return entryAssemblyPath;
        }

        return string.Empty;
    }
}