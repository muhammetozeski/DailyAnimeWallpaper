namespace DailyAnimeWallpaper.Localization;

/// <summary>
/// Routes the localization engine's original logging calls through the shared global Log method.
/// The engine keeps its existing recovery and fallback behavior.
/// </summary>
internal static class LocalizationLog
{
    private const bool IsLogEnabled = true;

    internal static void App(string message)
    {
        if (IsLogEnabled && LocManager.IsLogEnabled)
        {
            Log(message, isRun: IsLogEnabled && LocManager.IsLogEnabled);
        }
    }

    internal static void Error(string message, Exception exception)
    {
        if (IsLogEnabled && LocManager.IsLogEnabled)
        {
            Log($"{message}{Environment.NewLine}{exception}", isRun: IsLogEnabled && LocManager.IsLogEnabled);
        }
    }
}
