using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KjcBusinessHub.Application.Services;
using KjcBusinessHub.Infrastructure;
using KjcBusinessHub.UI.ViewModels;
using KjcBusinessHub.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using AvalApp = Avalonia.Application;

namespace KjcBusinessHub.UI;

public partial class App : AvalApp
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Use a fixed SQLite path in local app data
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KjcBusinessHub",
            "kjcbusinesshub.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddInfrastructure($"Data Source={dbPath}");
        services.AddLogging();

        // Register view models
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AppViewModel>();
        services.AddTransient<MainWindowViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        // Apply EF migrations synchronously on startup
        _serviceProvider.MigrateDatabaseAsync().GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>(),
            };

            desktop.Exit += (_, _) =>
            {
                _serviceProvider.GetService<FileWatcherService>()?.Stop();
                (_serviceProvider as IDisposable)?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}