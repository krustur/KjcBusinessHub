using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        var icon = new WindowIcon(AssetLoader.Open(new Uri("avares://KjcBusinessHub.UI/Assets/kjcbusinesshub-tray.ico")));

        var settingsItem = new NativeMenuItem("Settings");
        settingsItem.Click += (_, _) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _viewModel?.ShowSettings();
        };

        _closeToTrayMenuItem = new NativeMenuItem("Close to system tray")
        {
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = _settings.CloseToSystemTray,
        };
        _closeToTrayMenuItem.Click += OnToggleCloseToTrayClicked;

        var checkForUpdatesItem = new NativeMenuItem("Check for updates");
        checkForUpdatesItem.Click += async (_, _) =>
        {
            if (_updateService is not null)
            {
                await _updateService.CheckAndApplyUpdatesInBackgroundAsync();
            }
        };

        var aboutItem = new NativeMenuItem("About");
        aboutItem.Click += async (_, _) => await ShowAboutDialogAsync();

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += async (_, _) => await RequestQuitAsync();

        var menu = new NativeMenu
        {
            Items =
            {
                settingsItem,
                _closeToTrayMenuItem,
                new NativeMenuItemSeparator(),
                checkForUpdatesItem,
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
            Width = 500,
            Height = 300,
            CanResize = false,
            Title = "About KJC Business Hub",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = BuildAboutDialogContent(),
        };

        if (Icon is not null)
        {
            aboutWindow.Icon = Icon;
        }

        if (aboutWindow.Content is StackPanel panel)
        {
            var closeButton = panel.Children.OfType<Button>().FirstOrDefault();
            if (closeButton is not null)
            {
                closeButton.Click += (_, _) => aboutWindow.Close();
            }
        }

        await aboutWindow.ShowDialog(this);
    }

    private static StackPanel BuildAboutDialogContent()
    {
        var appFilePath = ResolveAppFilePath();
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

        var details = string.Join(Environment.NewLine, new[]
        {
            $"File Version: {fileVersion}",
            $"Product Version: {productVersion}",
            $"Copyright: {copyright}",
            $"Build Date/Time: {buildDateTimeText}",
        });

        return new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(18),
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
                },
                new Button
                {
                    Content = "Close",
                    Width = 90,
                    HorizontalAlignment = HorizontalAlignment.Right,
                }
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

        var candidateExe = Path.Combine(AppContext.BaseDirectory, "KjcBusinessHub.UI.exe");
        if (File.Exists(candidateExe))
        {
            return candidateExe;
        }

        var candidateDll = Path.Combine(AppContext.BaseDirectory, "KjcBusinessHub.UI.dll");
        if (File.Exists(candidateDll))
        {
            return candidateDll;
        }

        return string.Empty;
    }
}
