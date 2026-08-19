using System;
using System.Diagnostics;
using System.IO;

namespace KjcBusinessHub.UI;

public enum RuntimeMode
{
    Development,
    Production,
}

public sealed class RuntimeProfile
{
    public RuntimeProfile(RuntimeMode mode, string storageRoot)
    {
        Mode = mode;
        StorageRoot = storageRoot;
    }

    public RuntimeMode Mode { get; }
    public string StorageRoot { get; }
    public bool IsDevelopment => Mode == RuntimeMode.Development;
    public string DatabasePath => Path.Combine(StorageRoot, IsDevelopment ? "kjcbusinesshub.dev.db" : "kjcbusinesshub.db");
    public string SettingsPath => Path.Combine(StorageRoot, IsDevelopment ? "settings.dev.json" : "settings.json");
    public string LogsDirectory => Path.Combine(StorageRoot, "logs");
}

public static class RuntimeProfileDetector
{
    private const string RuntimeModeEnvironmentKey = "KJCBH_RUNTIME_MODE";

    public static RuntimeProfile Detect(string[] args)
    {
        var mode = ParseModeArgument(args)
            ?? ParseMode(Environment.GetEnvironmentVariable(RuntimeModeEnvironmentKey))
            ?? ParseMode(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
            ?? ParseMode(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            ?? (Debugger.IsAttached ? RuntimeMode.Development : RuntimeMode.Production);

        var appDirectoryName = mode == RuntimeMode.Development
            ? "KjcBusinessHub.Dev"
            : "KjcBusinessHub";

        var storageRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appDirectoryName);

        return new RuntimeProfile(mode, storageRoot);
    }

    private static RuntimeMode? ParseModeArgument(string[] args)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = arg.Substring("--mode=".Length);
            return ParseMode(value);
        }

        return null;
    }

    private static RuntimeMode? ParseMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "dev" or "development" => RuntimeMode.Development,
            "prod" or "production" => RuntimeMode.Production,
            _ => null,
        };
    }
}
