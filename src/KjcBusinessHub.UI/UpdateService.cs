using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace KjcBusinessHub.UI;

public sealed class UpdateService
{
    private const string UpdateChannelEnvironmentKey = "KJCBH_UPDATE_CHANNEL";
    private readonly RuntimeProfile _runtimeProfile;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(RuntimeProfile runtimeProfile, ILogger<UpdateService> logger)
    {
        _runtimeProfile = runtimeProfile;
        _logger = logger;
    }

    public async Task CheckAndApplyUpdatesInBackgroundAsync()
    {
        if (_runtimeProfile.IsDevelopment)
        {
            return;
        }

        try
        {
            var isPrereleaseChannel = IsPrereleaseChannel();
            var source = new GithubSource("https://github.com/krustur/KjcBusinessHub", null, prerelease: isPrereleaseChannel);
            var updateManager = new UpdateManager(source);

            if (!updateManager.IsInstalled)
            {
                return;
            }

            var update = await updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                return;
            }

            await updateManager.DownloadUpdatesAsync(update);
            updateManager.ApplyUpdatesAndRestart(update.TargetFullRelease);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed.");
        }
    }

    private static bool IsPrereleaseChannel()
    {
        var channel = Environment.GetEnvironmentVariable(UpdateChannelEnvironmentKey);
        return string.Equals(channel, "prerelease", StringComparison.OrdinalIgnoreCase);
    }
}
