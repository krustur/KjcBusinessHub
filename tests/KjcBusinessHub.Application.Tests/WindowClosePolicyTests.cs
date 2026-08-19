using KjcBusinessHub.UI;

namespace KjcBusinessHub.Application.Tests;

public class WindowClosePolicyTests
{
    [Fact]
    public void Close_to_tray_enabled_hides_window_to_tray()
    {
        var decision = WindowClosePolicy.Decide(closeToSystemTray: true);

        Assert.Equal(WindowCloseDecision.HideToTray, decision);
    }

    [Fact]
    public void Close_to_tray_disabled_requires_quit_confirmation()
    {
        var decision = WindowClosePolicy.Decide(closeToSystemTray: false);

        Assert.Equal(WindowCloseDecision.ConfirmQuit, decision);
    }
}
