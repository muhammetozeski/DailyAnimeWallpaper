namespace DailyAnimeWallpaper;

/// <summary>Identifies configuration messages forwarded to the host interface.</summary>
internal enum NotificationType
{
    Info,
    Warning
}

/// <summary>Connects the unchanged settings parser to the application log and optional host notifications.</summary>
internal static class NotificationService
{
    internal const bool IsLogEnabled = true;

    public static event Action<string, NotificationType>? Received;

    /// <summary>Records a configuration diagnostic and forwards it to subscribed host interfaces.</summary>
    /// <param name="message">The complete configuration diagnostic.</param>
    /// <param name="type">The severity presented by the host.</param>
    public static void Add(string message, NotificationType type = NotificationType.Info)
    {
        if (IsLogEnabled)
            Log($"[Settings] [{type}] {message}", isRun: IsLogEnabled);
        Received?.Invoke(message, type);
    }
}
