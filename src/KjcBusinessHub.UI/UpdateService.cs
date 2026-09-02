using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace KjcBusinessHub.UI;

public sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/krustur/KjcBusinessHub";
    private readonly RuntimeProfile _runtimeProfile;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(RuntimeProfile runtimeProfile, ILogger<UpdateService> logger)
    {
        _runtimeProfile = runtimeProfile;
        _logger = logger;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(UpdateChannel channel)
    {
        if (_runtimeProfile.IsDevelopment)
        {
            return new(UpdateCheckStatus.Unavailable, "Update checks are unavailable in development mode.");
        }

        try
        {
            var updateManager = CreateUpdateManager(channel);
            if (!updateManager.IsInstalled)
            {
                return new(UpdateCheckStatus.Unavailable, "Update checks are only available in installed builds.");
            }

            var update = await updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                return new(
                    UpdateCheckStatus.NoUpdateAvailable,
                    channel == UpdateChannel.Prerelease
                        ? "No pre-release update is currently available."
                        : "No update is currently available.");
            }

            var version = update.TargetFullRelease.Version.ToString();
            return new(
                UpdateCheckStatus.UpdateAvailable,
                $"Version {version} is available.",
                version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed.");
            return new(UpdateCheckStatus.Failed, BuildFailureMessage("check for updates", ex));
        }
    }

    public async Task<UpdateCheckResult> DownloadAndApplyUpdateAsync(UpdateChannel channel, string? expectedVersion = null)
    {
        if (_runtimeProfile.IsDevelopment)
        {
            return new(UpdateCheckStatus.Unavailable, "Update checks are unavailable in development mode.");
        }

        try
        {
            var updateManager = CreateUpdateManager(channel);
            if (!updateManager.IsInstalled)
            {
                return new(UpdateCheckStatus.Unavailable, "Update checks are only available in installed builds.");
            }

            var update = await updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                return new(
                    UpdateCheckStatus.NoUpdateAvailable,
                    channel == UpdateChannel.Prerelease
                        ? "No pre-release update is currently available."
                        : "No update is currently available.");
            }

            var version = update.TargetFullRelease.Version.ToString();
            if (!string.IsNullOrWhiteSpace(expectedVersion) &&
                !string.Equals(expectedVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                return new(
                    UpdateCheckStatus.UpdateAvailable,
                    $"Version {version} is available. Please confirm the update again.",
                    version);
            }

            await updateManager.DownloadUpdatesAsync(update);
            updateManager.ApplyUpdatesAndRestart(update.TargetFullRelease);
            return new(UpdateCheckStatus.UpdateApplied, $"Version {version} downloaded. Restarting to apply it.", version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update apply failed.");
            return new(UpdateCheckStatus.Failed, BuildFailureMessage("apply the update", ex));
        }
    }

    private static string BuildFailureMessage(string operation, Exception ex)
    {
        var detail = ex.GetBaseException().Message;
        return string.IsNullOrWhiteSpace(detail)
            ? $"Update failed while trying to {operation}."
            : $"Update failed while trying to {operation}.{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{detail}";
    }

    private static UpdateManager CreateUpdateManager(UpdateChannel channel)
    {
        var source = new GithubSource(RepositoryUrl, null, prerelease: channel == UpdateChannel.Prerelease);
        return new UpdateManager(
            source,
            new UpdateOptions
            {
                ExplicitChannel = channel == UpdateChannel.Prerelease ? "prerelease" : "stable",
                AllowVersionDowngrade = true,
            });
    }
}

public enum UpdateChannel
{
    Stable,
    Prerelease,
}

public enum UpdateCheckStatus
{
    UpdateAvailable,
    UpdateApplied,
    NoUpdateAvailable,
    Unavailable,
    Failed,
}

public sealed record UpdateCheckResult(UpdateCheckStatus Status, string Message, string? AvailableVersion = null);
