using Avalonia;
using System;
using System.Collections.Generic;
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
        MigrateLegacyDataIfNeeded(RuntimeProfile);
        MigrateDatabase(RuntimeProfile.DatabasePath);

        VelopackApp.Build().Run();
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

    private static void MigrateLegacyDataIfNeeded(RuntimeProfile runtimeProfile)
    {
        TryCopyLegacyFileIfTargetMissing(
            runtimeProfile.DatabasePath,
            GetLegacyFileCandidates(Path.GetFileName(runtimeProfile.DatabasePath)));
        TryCopyLegacyFileIfTargetMissing(
            runtimeProfile.SettingsPath,
            GetLegacyFileCandidates(Path.GetFileName(runtimeProfile.SettingsPath)));
    }

    private static void TryCopyLegacyFileIfTargetMissing(string targetPath, IEnumerable<string> candidatePaths)
    {
        if (File.Exists(targetPath))
        {
            return;
        }

        var normalizedTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar);
        string? bestCandidate = null;
        DateTime bestLastWriteUtc = DateTime.MinValue;

        foreach (var candidatePath in candidatePaths)
        {
            try
            {
                var normalizedCandidate = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar);
                if (string.Equals(normalizedCandidate, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!File.Exists(normalizedCandidate))
                {
                    continue;
                }

                var lastWriteUtc = File.GetLastWriteTimeUtc(normalizedCandidate);
                if (lastWriteUtc > bestLastWriteUtc)
                {
                    bestLastWriteUtc = lastWriteUtc;
                    bestCandidate = normalizedCandidate;
                }
            }
            catch
            {
                // Ignore unreadable legacy locations and continue probing.
            }
        }

        if (bestCandidate is null)
        {
            return;
        }

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        try
        {
            File.Copy(bestCandidate, targetPath, overwrite: false);
        }
        catch (IOException) when (File.Exists(targetPath))
        {
            // Another process/thread created the target file after our existence check.
        }
    }

    private static IEnumerable<string> GetLegacyFileCandidates(string fileName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in GetLegacyCandidateDirectories())
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string combinedPath;
            try
            {
                combinedPath = Path.GetFullPath(Path.Combine(directory, fileName));
            }
            catch
            {
                continue;
            }

            if (seen.Add(combinedPath))
            {
                yield return combinedPath;
            }
        }
    }

    private static IEnumerable<string> GetLegacyCandidateDirectories()
    {
        yield return AppContext.BaseDirectory;
        yield return Environment.CurrentDirectory;

        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            var processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
            if (!string.IsNullOrWhiteSpace(processDirectory))
            {
                yield return processDirectory;
            }
        }

        var appBaseParent = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(appBaseParent))
        {
            yield break;
        }

        yield return appBaseParent;

        IEnumerable<string> appVersionDirectories = Array.Empty<string>();
        try
        {
            appVersionDirectories = Directory.EnumerateDirectories(appBaseParent, "app-*");
        }
        catch
        {
            // Ignore missing/inaccessible parent directories.
        }

        foreach (var directory in appVersionDirectories)
        {
            yield return directory;
        }
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
