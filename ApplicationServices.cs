using System.Reflection;
using System.Xml.Linq;
using DailyAnimeWallpaper.Localization;
using static DailyAnimeWallpaper.ExceptionBoundary;

namespace DailyAnimeWallpaper;

/// <summary>Connects settings and language files at the application entry points without rewriting their engines.</summary>
internal static class ApplicationServices
{
    const bool IsLogEnabled = true;
    static readonly object InitializationLock = new();
    static bool Initialized;

    /// <summary>Loads portable settings and prepares both shipped languages once per process.</summary>
    /// <returns>The final initialization outcome; failures have already been logged and contained.</returns>
    public static OperationResult<bool> Initialize() => RunSafely(() =>
    {
        lock (InitializationLock)
        {
            if (Initialized)
                return;

            Directory.CreateDirectory(AppPaths.UserData);
            SettingsManager.LoadSettings(ConfigFileName);
            Logger.ActivateLogging = Settings.EnableLogging.Value;
            LocManager.Init(Settings.Language.Value);
            SettingsManagerSettings.IsTurkish = LocManager.Current == "tr";
            ValidateLanguageFile("tr");
            ValidateLanguageFile("en");
            ValidateAppliedLanguage(LocManager.Resolve(Settings.Language.Value));
            Initialized = true;
            if (IsLogEnabled)
                Log($"Application initialized; root={AppPaths.Root}; language={LocManager.Current}; settings={SettingsManager.GetAllSettings().Length}", isRun: IsLogEnabled);
        }
    });

    /// <summary>Saves the registered settings through the common final failure boundary.</summary>
    /// <returns>The save outcome; the caller must check Succeeded before displaying success.</returns>
    public static OperationResult<bool> SaveSettings() => RunSafely(() =>
    {
        Directory.CreateDirectory(AppPaths.UserData);
        SettingsManager.SaveSettings(ConfigFileName);
        Logger.ActivateLogging = Settings.EnableLogging.Value;
        if (IsLogEnabled)
            Log($"Settings saved; path={ConfigFileName}; count={SettingsManager.GetAllSettings().Length}", isRun: IsLogEnabled);
    });

    /// <summary>Applies a complete language file and optionally saves the preference for the next launch.</summary>
    /// <param name="language">A language code or system to follow the operating system language.</param>
    /// <param name="persist">Whether the selection should be stored in portable settings.</param>
    /// <returns>The applied language code or the contained failure.</returns>
    public static OperationResult<string> ChangeLanguage(string language, bool persist = true) => RunSafely(() =>
    {
        string resolvedLanguage = LocManager.Resolve(language);
        ValidateLanguageFile(resolvedLanguage);
        LocManager.Apply(language);
        SettingsManagerSettings.IsTurkish = LocManager.Current == "tr";
        ValidateAppliedLanguage(resolvedLanguage);
        if (persist)
        {
            Settings.Language.Value = language;
            SettingsManager.SaveSettings(ConfigFileName);
        }
        if (IsLogEnabled)
            Log($"Language changed; selection={language}; active={LocManager.Current}; persisted={persist}", isRun: IsLogEnabled);
        return LocManager.Current;
    });

    /// <summary>Rejects incomplete language files before they can leave a mix of old and new strings.</summary>
    /// <param name="language">The resolved culture code.</param>
    /// <returns>The language values indexed by field name.</returns>
    static Dictionary<string, string> ValidateLanguageFile(string language)
    {
        _ = System.Globalization.CultureInfo.GetCultureInfo(language);
        string path = Path.Combine(AppPaths.UserData, $"lang.{language}.xml");
        var document = XDocument.Load(path);
        if (document.Root?.Name != "strings")
            throw new InvalidDataException($"Invalid language document: {path}");
        var values = document.Root.Elements("s").ToDictionary(
            element => element.Attribute("name")?.Value ?? throw new InvalidDataException($"A language entry has no name: {path}"),
            element => element.Value.Replace("\\n", "\n"), StringComparer.Ordinal);
        foreach (var field in typeof(Strings).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.FieldType == typeof(string) && !values.ContainsKey(field.Name))
                throw new InvalidDataException($"Language {language} is missing {field.Name}.");
        return values;
    }

    /// <summary>Checks the result because the preserved localization engine contains some failures internally.</summary>
    /// <param name="language">The language expected after the engine returns.</param>
    static void ValidateAppliedLanguage(string language)
    {
        var values = ValidateLanguageFile(language);
        if (LocManager.Current != language)
            throw new InvalidDataException($"Expected language {language}, received {LocManager.Current}.");
        foreach (var field in typeof(Strings).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.FieldType == typeof(string) && !Equals(field.GetValue(null), values[field.Name]))
                throw new InvalidDataException($"Language {language} was not applied to {field.Name}.");
    }
}
