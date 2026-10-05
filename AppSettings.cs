using System.Text.Json;

namespace DailyAnimeWallpaper;

/// <summary>
/// Loads the wallpaper image directory from the settings file beside the executable.
/// </summary>
internal sealed class AppSettings
{
    private const string SettingsFileName = "appsettings.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Gets or sets the image directory. Relative paths are resolved from the application directory.
    /// </summary>
    public string ImageSaveDirectory { get; set; } = "pictures";

    /// <summary>
    /// Reads settings beside the executable and creates the default file when it is missing.
    /// </summary>
    /// <returns>The settings loaded from disk.</returns>
    public static AppSettings Load()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
        if (!File.Exists(settingsPath))
        {
            var defaults = new AppSettings();
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(defaults, JsonOptions));
            return defaults;
        }

        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath), JsonOptions)
            ?? throw new InvalidDataException($"The settings file '{settingsPath}' is empty or invalid.");
    }

    /// <summary>
    /// Resolves the configured directory to an absolute path.
    /// </summary>
    /// <returns>The absolute directory used to save downloaded images.</returns>
    public string GetImageSaveDirectory()
    {
        if (string.IsNullOrWhiteSpace(ImageSaveDirectory))
            throw new InvalidDataException($"'{nameof(ImageSaveDirectory)}' must contain a path.");

        return Path.GetFullPath(ImageSaveDirectory, AppContext.BaseDirectory);
    }
}
