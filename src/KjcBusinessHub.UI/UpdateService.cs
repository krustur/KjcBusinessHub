using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace KjcBusinessHub.UI;

public sealed class UpdateService
{
    private const string UpdateChannelEnvironmentKey = "KJCBH_UPDATE_CHANNEL";
    private const string RepositoryUrl = "https://github.com/krustur/KjcBusinessHub";
    private const string VelopackLogFileName = "velopack.log";
    private readonly RuntimeProfile _runtimeProfile;
    private readonly ILogger<UpdateService> _logger;
    private readonly UpdateAttemptTracker _updateAttemptTracker;

    public UpdateService(RuntimeProfile runtimeProfile, ILogger<UpdateService> logger)
    {
        _runtimeProfile = runtimeProfile;
        _logger = logger;
        _updateAttemptTracker = new UpdateAttemptTracker(runtimeProfile.StorageRoot);
    }

    public string? ConsumePendingFailureNotification()
    {
        ResolvePendingUpdateAttempt();
        return _updateAttemptTracker.ConsumePendingFailureNotification();
    }

    public async Task<UpdateCheckResult?> CheckAndApplyUpdatesInBackgroundAsync()
    {
        ResolvePendingUpdateAttempt();

        var result = await CheckAndApplyUpdatesCoreAsync(
            IsPrereleaseChannel() ? UpdateChannel.Prerelease : UpdateChannel.Stable,
            suppressKnownFailureMessage: true);

        return result.Status == UpdateCheckStatus.Failed ? result : null;
    }

    public async Task<UpdateCheckResult> CheckAndApplyUpdatesAsync(UpdateChannel channel)
    {
        ResolvePendingUpdateAttempt();
        return await CheckAndApplyUpdatesCoreAsync(channel, suppressKnownFailureMessage: false);
    }

    private async Task<UpdateCheckResult> CheckAndApplyUpdatesCoreAsync(
        UpdateChannel channel,
        bool suppressKnownFailureMessage)
    {
        if (_runtimeProfile.IsDevelopment)
        {
            return new(UpdateCheckStatus.Unavailable, "Update checks are unavailable in development mode.");
        }

        var operation = "checking for updates";
        string? targetVersion = null;

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

            targetVersion = update.TargetFullRelease.Version.ToString();
            var previousFailure = _updateAttemptTracker.GetFailureMessageForVersion(targetVersion);
            if (!string.IsNullOrWhiteSpace(previousFailure))
            {
                _logger.LogWarning("Skipping previously failed update attempt for version {TargetVersion}.", targetVersion);
                return suppressKnownFailureMessage
                    ? new(UpdateCheckStatus.NoUpdateAvailable, previousFailure)
                    : new(UpdateCheckStatus.Failed, previousFailure);
            }

            operation = $"downloading update {targetVersion}";
            await updateManager.DownloadUpdatesAsync(update);

            operation = $"starting update {targetVersion}";
            _updateAttemptTracker.RecordPendingAttempt(targetVersion);
            updateManager.ApplyUpdatesAndRestart(update.TargetFullRelease);
            return new(UpdateCheckStatus.UpdateApplied, "Update downloaded. Restarting to apply it.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update flow failed while {Operation}.", operation);
            var message = BuildOperationFailureMessage(operation, ex);
            if (!string.IsNullOrWhiteSpace(targetVersion))
            {
                _updateAttemptTracker.RecordImmediateFailure(targetVersion, message);
            }

            return new(UpdateCheckStatus.Failed, message);
        }
    }

    private void ResolvePendingUpdateAttempt()
    {
        var failure = _updateAttemptTracker.ResolvePendingAttempt(GetCurrentVersion(), TryReadRecentVelopackFailureDetails());
        if (!string.IsNullOrWhiteSpace(failure))
        {
            _logger.LogWarning("Detected a failed restart while applying an update.");
        }
    }

    private static string BuildOperationFailureMessage(string operation, Exception ex)
    {
        var detail = ex.GetBaseException().Message;
        return string.IsNullOrWhiteSpace(detail)
            ? $"Update failed while {operation}."
            : $"Update failed while {operation}.{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{detail}";
    }

    private static string GetCurrentVersion()
    {
        try
        {
            return VelopackLocator.Current.CurrentlyInstalledVersion?.ToString()
                ?? typeof(UpdateService).Assembly.GetName().Version?.ToString()
                ?? "unknown";
        }
        catch
        {
            return typeof(UpdateService).Assembly.GetName().Version?.ToString() ?? "unknown";
        }
    }

    private static string? TryReadRecentVelopackFailureDetails()
    {
        try
        {
            var appTempDir = VelopackLocator.Current.AppTempDir;
            if (string.IsNullOrWhiteSpace(appTempDir))
            {
                return null;
            }

            var logPath = Path.Combine(appTempDir, VelopackLogFileName);
            if (!File.Exists(logPath))
            {
                return null;
            }

            var relevantLines = new Queue<string>();
            foreach (var line in File.ReadLines(logPath))
            {
                if (!LooksRelevant(line))
                {
                    continue;
                }

                if (relevantLines.Count == 6)
                {
                    relevantLines.Dequeue();
                }

                relevantLines.Enqueue(line.Trim());
            }

            return relevantLines.Count == 0 ? null : string.Join(Environment.NewLine, relevantLines);
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksRelevant(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("exception", StringComparison.OrdinalIgnoreCase);
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
