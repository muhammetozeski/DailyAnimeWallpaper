namespace DailyAnimeWallpaper;

/// <summary>Contains application identity and file-format values shared by all features.</summary>
internal static class AppConstants
{
    internal const bool IsLogEnabled = true;

    public const bool PublishMode = false;
    public const string AppTitle = "DailyAnimeWallpaper";
    public const string CommentPrefix = "#";
    public const string KeyValueSeparator = "=";

    public static readonly string ThisExePath = Environment.ProcessPath!;
    public static readonly string ThisExeFolder = Path.GetDirectoryName(ThisExePath)!;
    public static readonly string ConfigFileName = Path.Combine(AppPaths.UserData, "settings.txt");
}
