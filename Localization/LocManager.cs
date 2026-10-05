using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using DailyAnimeWallpaper;

namespace DailyAnimeWallpaper.Localization;

/// <summary>
/// Drives localization for <see cref="Strings"/>. On startup it ensures <c>lang.tr.xml</c> exists
/// (writing the Turkish defaults so the user can edit them), extracts the shipped
/// <c>lang.en.xml</c>, and — when another language is chosen — loads <c>lang.&lt;code&gt;.xml</c>
/// over the fields via reflection.
///
/// Dropping another <c>lang.&lt;code&gt;.xml</c> into the configuration folder is enough to add a
/// language; it shows up in the picker without a code change.
/// </summary>
public static class LocManager
{
    internal const bool IsLogEnabled = true;
    public const string SystemLanguage = "system";
    private const string EnglishResource = "DailyAnimeWallpaper.Localization.lang.en.xml";

    /// <summary>Fires after the active language changes so open windows can re-apply their text.</summary>
    public static event Action? LanguageChanged;

    public static string Current { get; private set; } = "tr";

    private static string Folder => AppPaths.UserData;

    public static (string Code, string Name)[] Available
    {
        get
        {
            try
            {
                if (!Directory.Exists(Folder))
                {
                    return [("tr", "Türkçe")];
                }

                var found = Directory.GetFiles(Folder, "lang.*.xml")
                    .Select(file =>
                    {
                        var parts = Path.GetFileName(file).Split('.');
                        var code = parts.Length > 1 ? parts[1] : "??";
                        var name = code.ToUpperInvariant();

                        try
                        {
                            var doc = XDocument.Load(file);
                            var languageName = doc.Root?.Element("LanguageName")?.Value;
                            if (!string.IsNullOrWhiteSpace(languageName))
                            {
                                name = languageName;
                            }
                        }
                        catch (Exception ex)
                        {
                            LocalizationLog.App($"Could not read the language name from {Path.GetFileName(file)}: {ex.GetType().Name}");
                        }

                        return (Code: code, Name: name);
                    })
                    .OrderBy(item => item.Name, StringComparer.CurrentCulture)
                    .ToArray();

                return found.Length > 0 ? found : [("tr", "Türkçe")];
            }
            catch (Exception ex)
            {
                LocalizationLog.Error("Could not list the available languages", ex);
                return [("tr", "Türkçe")];
            }
        }
    }

    private static FieldInfo[] StringFields() => typeof(Strings)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(string))
        .ToArray();

    private static string PathFor(string language) => Path.Combine(Folder, $"lang.{language}.xml");

    /// <summary>
    /// Prepares the language files and applies the configured language. Pass "system" to follow the
    /// Windows display language, which is what a first run does.
    /// </summary>
    public static void Init(string configuredLanguage)
    {
        try
        {
            Directory.CreateDirectory(Folder);

            // The shipped files are rewritten when the application version changes.
            //
            // They are written once and then loaded over the compiled-in text, so without this a
            // wording change in a new build never reaches the screen: the stale file wins and the
            // interface silently keeps the old version's text. A copy of the previous file is kept
            // alongside so hand edits are not simply thrown away.
            var currentVersion = AppPaths.PayloadVersion;

            RefreshShippedFile(PathFor("tr"), currentVersion, () => WriteXml(PathFor("tr"), "Türkçe", currentVersion));
            RefreshShippedFile(PathFor("en"), currentVersion, () => ExtractEnglish(currentVersion));
        }
        catch (Exception ex)
        {
            LocalizationLog.Error("Preparing the language files failed", ex);
        }

        Apply(configuredLanguage);
    }

    private static void RefreshShippedFile(string path, string currentVersion, Action write)
    {
        try
        {
            if (!File.Exists(path))
            {
                write();
                return;
            }

            var stamped = ReadVersion(path);
            if (stamped == currentVersion)
            {
                return;
            }

            var backup = path + ".previous";
            File.Copy(path, backup, overwrite: true);
            LocalizationLog.App($"{Path.GetFileName(path)} was written by version {stamped ?? "unknown"}; refreshing it and keeping the old one as {Path.GetFileName(backup)}");

            write();
        }
        catch (Exception ex)
        {
            LocalizationLog.Error($"Could not refresh {Path.GetFileName(path)}", ex);
        }
    }

    private static string? ReadVersion(string path)
    {
        try
        {
            return XDocument.Load(path).Root?.Element("Version")?.Value;
        }
        catch (Exception ex)
        {
            LocalizationLog.App($"Could not read the version stamp from {Path.GetFileName(path)}: {ex.GetType().Name}");
            return null;
        }
    }

    /// <summary>Loads a language over the <see cref="Strings"/> fields and notifies listeners.</summary>
    public static void Apply(string configuredLanguage)
    {
        var language = Resolve(configuredLanguage);

        try
        {
            if (language == "tr")
            {
                // The fields may hold another language after a switch, and lang.tr.xml is editable,
                // so it is read back whenever it exists. Skipping it would keep the previous
                // language on screen and silently discard the user's own wording.
                var turkishPath = PathFor("tr");
                if (File.Exists(turkishPath))
                {
                    ReadInto(turkishPath);
                }
            }
            else
            {
                var path = PathFor(language);
                if (File.Exists(path))
                {
                    ReadInto(path);
                }
                else
                {
                    LocalizationLog.App($"Language file not found: {path}; keeping the Turkish defaults.");
                    language = "tr";
                }
            }

            Current = language;

            var culture = new CultureInfo(language);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (Exception ex)
        {
            LocalizationLog.Error($"Applying the language '{language}' failed", ex);
        }

        try
        {
            LanguageChanged?.Invoke();
        }
        catch (Exception ex)
        {
            LocalizationLog.Error("A LanguageChanged handler threw", ex);
        }
    }

    /// <summary>Turns "system" into a concrete language code against the Windows display language.</summary>
    public static string Resolve(string configuredLanguage)
    {
        var configured = (configuredLanguage ?? string.Empty).Trim().ToLowerInvariant();

        if (configured.Length > 0 && configured != SystemLanguage)
        {
            return configured;
        }

        try
        {
            var system = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (File.Exists(PathFor(system)))
            {
                return system;
            }

            // A first run happens before the files are written, so fall back to the two languages
            // that always exist.
            if (system is "tr" or "en")
            {
                return system;
            }
        }
        catch (Exception ex)
        {
            LocalizationLog.Error("Could not read the Windows display language", ex);
        }

        return "en";
    }

    /// <summary>Reflection-writes the current field values to an XML file.</summary>
    private static void WriteXml(string path, string languageName, string version)
    {
        try
        {
            var doc = new XDocument(new XElement("strings",
                new XElement("LanguageName", languageName),
                new XElement("Version", version),
                StringFields().Select(f => new XElement("s",
                    new XAttribute("name", f.Name),
                    Escape((string?)f.GetValue(null) ?? string.Empty)))));

            File.WriteAllText(path, doc.ToString(), new UTF8Encoding(false));
            LocalizationLog.App($"Wrote the language baseline {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            LocalizationLog.Error("Writing the language baseline failed", ex);
        }
    }

    /// <summary>Reflection-loads values from an XML file over the matching fields.</summary>
    private static void ReadInto(string path)
    {
        try
        {
            var doc = XDocument.Load(path);
            var map = StringFields().ToDictionary(f => f.Name, StringComparer.Ordinal);
            var count = 0;

            foreach (var element in doc.Root?.Elements("s") ?? [])
            {
                var name = element.Attribute("name")?.Value;
                if (name is not null && map.TryGetValue(name, out var field))
                {
                    field.SetValue(null, Unescape(element.Value));
                    count++;
                }
            }

            LocalizationLog.App($"Loaded {count} localized string(s) from {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            LocalizationLog.Error($"Reading {Path.GetFileName(path)} failed", ex);
        }
    }

    private static void ExtractEnglish(string version)
    {
        var path = PathFor("en");

        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EnglishResource);
            if (stream is null)
            {
                LocalizationLog.App($"The embedded resource {EnglishResource} is missing; English is unavailable.");
                return;
            }

            var doc = XDocument.Load(stream);

            // The stamp is added on the way out rather than kept in the source file, so it cannot
            // drift from the version that actually shipped it.
            doc.Root?.Element("Version")?.Remove();
            doc.Root?.AddFirst(new XElement("Version", version));

            doc.Save(path);
            LocalizationLog.App("Wrote lang.en.xml");
        }
        catch (Exception ex)
        {
            LocalizationLog.Error("Extracting lang.en.xml failed", ex);
        }
    }

    // Multi-line strings are stored single-line with a literal \n so the XML stays clean/indentable.
    private static string Escape(string s) => s.Replace("\r\n", "\n").Replace("\n", "\\n");

    private static string Unescape(string s) => s.Replace("\\n", "\n");
}
