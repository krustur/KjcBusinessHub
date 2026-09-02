using Avalonia;
using System;
using System.IO;
using KjcBusinessHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Velopack;

namespace KjcBusinessHub.UI;

sealed class Program
{
    public static RuntimeProfile RuntimeProfile { get; private set; } =
        new(RuntimeMode.Production, AppContext.BaseDirectory);

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        RuntimeProfile = RuntimeProfileDetector.Detect(args);

        // Ensure the storage directory exists and run database migrations before
        // VelopackApp.Build().Run(), which may exit the process early when handling
        // lifecycle hooks (--velopack-install, --velopack-updated, etc.).
        // Running migrations here guarantees the schema is always up to date,
        // even when the process terminates after hook handling.
        Directory.CreateDirectory(RuntimeProfile.StorageRoot);
        MigrateDatabase(RuntimeProfile.DatabasePath);

        VelopackApp.Build()
            .SetAutoApplyOnStartup(false)
            .Run();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void MigrateDatabase(string dbPath)
    {
        // Intentionally mirrors the options used by the DI-registered AppDbContext in
        // ServiceCollectionExtensions.AddInfrastructure (UseSqlite only, no extra options).
        // Keep both in sync if additional DbContext options are ever added.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        using var db = new AppDbContext(options);
        db.Database.Migrate();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
