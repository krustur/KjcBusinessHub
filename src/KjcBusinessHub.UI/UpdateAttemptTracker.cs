using System;
using System.IO;
using System.Text.Json;

namespace KjcBusinessHub.UI;

internal sealed class UpdateAttemptTracker
{
    private const string StateFileName = "update-state.json";
    private readonly string _stateFilePath;

    public UpdateAttemptTracker(string storageRoot)
    {
        Directory.CreateDirectory(storageRoot);
        _stateFilePath = Path.Combine(storageRoot, StateFileName);
    }

    public static void MarkRestarted(string storageRoot, string restartedVersion)
    {
        var tracker = new UpdateAttemptTracker(storageRoot);
        tracker.MarkRestarted(restartedVersion);
    }

    public void RecordPendingAttempt(string targetVersion)
    {
        var state = Load();
        state.PendingTargetVersion = targetVersion;
        Save(state);
    }

    public void RecordImmediateFailure(string targetVersion, string message)
    {
        var state = Load();
        state.PendingTargetVersion = null;
        state.LastFailedVersion = targetVersion;
        state.LastFailureMessage = message;
        state.FailureNotificationPending = true;
        Save(state);
    }

    public string? ResolvePendingAttempt(string currentVersion, string? failureDetails)
    {
        var state = Load();
        if (string.IsNullOrWhiteSpace(state.PendingTargetVersion))
        {
            return null;
        }

        if (string.Equals(state.PendingTargetVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            state.PendingTargetVersion = null;
            if (string.Equals(state.LastFailedVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
            {
                state.LastFailedVersion = null;
                state.LastFailureMessage = null;
                state.FailureNotificationPending = false;
            }

            Save(state);
            return null;
        }

        var message = BuildRestartFailureMessage(state.PendingTargetVersion, failureDetails);
        state.LastFailedVersion = state.PendingTargetVersion;
        state.LastFailureMessage = message;
        state.FailureNotificationPending = true;
        state.PendingTargetVersion = null;
        Save(state);
        return message;
    }

    public string? ConsumePendingFailureNotification()
    {
        var state = Load();
        if (!state.FailureNotificationPending || string.IsNullOrWhiteSpace(state.LastFailureMessage))
        {
            return null;
        }

        state.FailureNotificationPending = false;
        Save(state);
        return state.LastFailureMessage;
    }

    public string? GetFailureMessageForVersion(string targetVersion)
    {
        var state = Load();
        return string.Equals(state.LastFailedVersion, targetVersion, StringComparison.OrdinalIgnoreCase)
            ? state.LastFailureMessage
            : null;
    }

    private void MarkRestarted(string restartedVersion)
    {
        var state = Load();
        state.PendingTargetVersion = null;
        if (string.Equals(state.LastFailedVersion, restartedVersion, StringComparison.OrdinalIgnoreCase))
        {
            state.LastFailedVersion = null;
            state.LastFailureMessage = null;
            state.FailureNotificationPending = false;
        }

        Save(state);
    }

    private UpdateAttemptState Load()
    {
        if (!File.Exists(_stateFilePath))
        {
            return new();
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<UpdateAttemptState>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void Save(UpdateAttemptState state)
    {
        if (string.IsNullOrWhiteSpace(state.PendingTargetVersion) &&
            string.IsNullOrWhiteSpace(state.LastFailedVersion) &&
            string.IsNullOrWhiteSpace(state.LastFailureMessage) &&
            !state.FailureNotificationPending)
        {
            if (File.Exists(_stateFilePath))
            {
                File.Delete(_stateFilePath);
            }

            return;
        }

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_stateFilePath, json);
    }

    private static string BuildRestartFailureMessage(string targetVersion, string? failureDetails)
    {
        var message = $"Update to version {targetVersion} failed. The app started without applying the update.";
        if (string.IsNullOrWhiteSpace(failureDetails))
        {
            return message;
        }

        return $"{message}{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{failureDetails}";
    }

    private sealed class UpdateAttemptState
    {
        public string? PendingTargetVersion { get; set; }
        public string? LastFailedVersion { get; set; }
        public string? LastFailureMessage { get; set; }
        public bool FailureNotificationPending { get; set; }
    }
}
