using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KjcBusinessHub.Application.Services;
using KjcBusinessHub.Infrastructure;
using KjcBusinessHub.UI.ViewModels;
using KjcBusinessHub.UI.Views;
using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
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
        var runtime = Program.RuntimeProfile;
        var appDataDir = runtime.StorageRoot;
        Directory.CreateDirectory(appDataDir);

        // Rolling file: one file per day, keep 7 days
        var logPath = Path.Combine(appDataDir, "logs", "kjcbusinesshub-.log");
        Log.Logger = new LoggerConfiguration()
            // .MinimumLevel.Debug()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var services = new ServiceCollection();

        // Use a fixed SQLite path in local app data
        var dbPath = runtime.DatabasePath;
        var settingsPath = runtime.SettingsPath;

        services.AddInfrastructure($"Data Source={dbPath}", settingsPath);
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(dispose: true);
        });

        services.AddSingleton(runtime);

        // Register view models
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AppViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<UpdateService>();

        _serviceProvider = services.BuildServiceProvider();

        // Apply EF migrations synchronously on startup
        _serviceProvider.MigrateDatabaseAsync().GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };

            var mainWindow = (MainWindow)desktop.MainWindow;
            if (runtime.IsDevelopment)
            {
                mainWindow.Title = string.IsNullOrWhiteSpace(mainWindow.Title)
                    ? "KJC Business Hub (Development)"
                    : $"{mainWindow.Title} (Development)";
            }

            mainWindow.Configure(
                _serviceProvider.GetRequiredService<MainWindowViewModel>(),
                _serviceProvider.GetRequiredService<ISettingsService>(),
                _serviceProvider.GetRequiredService<UpdateService>());

            _ = _serviceProvider.GetRequiredService<UpdateService>().CheckAndApplyUpdatesInBackgroundAsync();

            desktop.Exit += (_, _) =>
            {
                _serviceProvider.GetService<FileWatcherService>()?.Stop();
                (_serviceProvider as IDisposable)?.Dispose();
                Log.CloseAndFlush();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}