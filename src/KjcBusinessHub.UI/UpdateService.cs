using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace KjcBusinessHub.UI;

public sealed class UpdateService
{
    private const string UpdateChannelEnvironmentKey = "KJCBH_UPDATE_CHANNEL";
    private const string RepositoryUrl = "https://github.com/krustur/KjcBusinessHub";
    private readonly RuntimeProfile _runtimeProfile;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(RuntimeProfile runtimeProfile, ILogger<UpdateService> logger)
    {
        _runtimeProfile = runtimeProfile;
        _logger = logger;
    }

    public async Task CheckAndApplyUpdatesInBackgroundAsync()
    {
        await CheckAndApplyUpdatesAsync(IsPrereleaseChannel() ? UpdateChannel.Prerelease : UpdateChannel.Stable);
    }

    public async Task<UpdateCheckResult> CheckAndApplyUpdatesAsync(UpdateChannel channel)
    {
        if (_runtimeProfile.IsDevelopment)
        {
            return new(UpdateCheckStatus.Unavailable, "Update checks are unavailable in development mode.");
        }

        try
        {
            var source = new GithubSource(RepositoryUrl, null, prerelease: channel == UpdateChannel.Prerelease);
            var updateManager = new UpdateManager(
                source,
                new UpdateOptions
                {
                    ExplicitChannel = channel == UpdateChannel.Prerelease ? "prerelease" : "stable",
                    AllowVersionDowngrade = true,
                });

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

            await updateManager.DownloadUpdatesAsync(update);
            updateManager.ApplyUpdatesAndRestart(update.TargetFullRelease);
            return new(UpdateCheckStatus.UpdateApplied, "Update downloaded. Restarting to apply it.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed.");
            return new(UpdateCheckStatus.Failed, "Update check failed. Please try again later.");
        }
    }

    private static bool IsPrereleaseChannel()
    {
        var channel = Environment.GetEnvironmentVariable(UpdateChannelEnvironmentKey);
        return string.Equals(channel, "prerelease", StringComparison.OrdinalIgnoreCase);
    }
}

public enum UpdateChannel
{
    Stable,
    Prerelease,
}

public enum UpdateCheckStatus
{
    UpdateApplied,
    NoUpdateAvailable,
    Unavailable,
    Failed,
}

public sealed record UpdateCheckResult(UpdateCheckStatus Status, string Message);
