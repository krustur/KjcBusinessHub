namespace KjcBusinessHub.UI;

public enum WindowCloseDecision
{
    HideToTray,
    ConfirmQuit,
}

public static class WindowClosePolicy
{
    public static WindowCloseDecision Decide(bool closeToSystemTray) =>
        closeToSystemTray
            ? WindowCloseDecision.HideToTray
            : WindowCloseDecision.ConfirmQuit;
}
