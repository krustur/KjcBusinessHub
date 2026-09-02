using System;
using System.IO;
using KjcBusinessHub.UI;

namespace KjcBusinessHub.Application.Tests.Services;

public class UpdateAttemptTrackerTests
{
    [Fact]
    public void ResolvePendingAttempt_records_failure_and_emits_notification_once()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sut = new UpdateAttemptTracker(tempDirectory);
            sut.RecordPendingAttempt("1.0.2-beta");

            var message = sut.ResolvePendingAttempt("1.0.1-beta", "permission denied");

            Assert.NotNull(message);
            Assert.Contains("1.0.2-beta", message, StringComparison.Ordinal);
            Assert.Contains("permission denied", message, StringComparison.Ordinal);
            Assert.Equal(message, sut.GetFailureMessageForVersion("1.0.2-beta"));
            Assert.Equal(message, sut.ConsumePendingFailureNotification());
            Assert.Null(sut.ConsumePendingFailureNotification());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ResolvePendingAttempt_clears_state_when_update_restarted_into_target_version()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sut = new UpdateAttemptTracker(tempDirectory);
            sut.RecordPendingAttempt("1.0.2-beta");

            var message = sut.ResolvePendingAttempt("1.0.2-beta", "ignored");

            Assert.Null(message);
            Assert.Null(sut.ConsumePendingFailureNotification());
            Assert.Null(sut.GetFailureMessageForVersion("1.0.2-beta"));
            Assert.False(File.Exists(Path.Combine(tempDirectory, "update-state.json")));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void MarkRestarted_clears_pending_attempt_after_successful_restart()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sut = new UpdateAttemptTracker(tempDirectory);
            sut.RecordPendingAttempt("1.0.2-beta");

            UpdateAttemptTracker.MarkRestarted(tempDirectory, "1.0.2-beta");

            Assert.Null(sut.ResolvePendingAttempt("1.0.2-beta", null));
            Assert.Null(sut.ConsumePendingFailureNotification());
            Assert.False(File.Exists(Path.Combine(tempDirectory, "update-state.json")));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kjcbh-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
