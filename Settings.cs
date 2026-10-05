using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DailyAnimeWallpaper;

internal static class SettingsManagerSettings
{
    public const string ConfigFileName = "config.txt";
    public const string CommentPrefix = "#";
    public const string KeyValueSeparator = "=";
    public static readonly string AppTitle = Assembly.GetEntryAssembly()?.GetName().Name ?? Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "Application";
    public static readonly string ThisExeFolder = AppContext.BaseDirectory;
    public static bool IsTurkish;
    public const string DescriptionFormatEnglish = "{0} expects {1}. Default: {2}.";
    public const string DescriptionFormatTurkish = "{0} için {1} girin. Varsayılan değer: {2}.";
    public const string ExampleFormatEnglish = " Example: {0}.";
    public const string ExampleFormatTurkish = " Örnek: {0}.";
    public const string OptionalDescriptionEnglish = " You may leave it blank.";
    public const string OptionalDescriptionTurkish = " Boş bırakabilirsiniz.";
}

// Declare settings here. Field names become file keys automatically.
internal static class Settings
{
    public static readonly Setting<string> Language = new("system");
    public static readonly Setting<bool> EnableLogging = new(true);
    public static readonly Setting<string> ImageSaveDirectory = new("pictures", isOptional: false);
}

internal interface ISetting
{
    string Key { get; }
    string KeyHumanReadable { get; }
    string DisplayName { get; }
    string Description { get; }
    string? ValueExample { get; }
    bool IsOptional { get; }
    Type ValueType { get; }
    object? Value { get; set; }
    object? DefaultValue { get; }
    bool IsDefault { get; }
    bool TrySetFromText(string text);
    void LoadFromStr(string value);
    void ResetToDefault();
}

internal interface ISettingSetup
{
    string Key { get; }
    void InitializeKey(string key);
    void LoadFromStr(string value);
    bool TryLoadSerialized(string value);
    string Serialize();
}

internal class Setting<T> : ISetting, ISettingSetup
{
    private static readonly Type ParsedType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
    private static readonly TypeConverter Converter = TypeDescriptor.GetConverter(ParsedType);
    private static readonly bool CanReadText = Converter.CanConvertFrom(typeof(string));
    private static readonly bool CanWriteText = Converter.CanConvertTo(typeof(string));
    private static readonly bool AllowsNull = !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null;
    private static readonly string TypeName = SettingsManager.SplitPascalCase(ParsedType.Name);
    private static readonly InterfaceMapping? ParsableMap = ParsedType.GetInterfaces().FirstOrDefault(contract =>
        contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IParsable<>)
        && contract.GetGenericArguments()[0] == ParsedType) is { } contract ? ParsedType.GetInterfaceMap(contract) : null;
    private static readonly MethodInfo? ParseMethod = ParsableMap is { } map
        ? map.TargetMethods[Array.FindIndex(map.InterfaceMethods, method => method.Name == "Parse")] : null;
    private readonly string? _displayName;
    private readonly string? _valueExample;
    private readonly string? _description;

    /// <param name="defaultValue">Initial value and fallback for an invalid file value.</param>
    /// <param name="description">Optional explanation; generated from the other metadata when blank.</param>
    /// <param name="displayName">Optional label; the field name is otherwise split into words.</param>
    /// <param name="valueExample">Optional input example; the default value is used when blank.</param>
    /// <param name="isOptional">Whether text input may be blank or null when the type allows it.</param>
    public Setting(T defaultValue, string? description = null, string? displayName = null, string? valueExample = null, bool isOptional = true)
    {
        Value = defaultValue;
        DefaultValue = defaultValue;
        _description = description;
        _displayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
        _valueExample = valueExample;
        IsOptional = isOptional;
    }

    public string Key { get; private set; } = string.Empty;
    public string KeyHumanReadable { get; private set; } = string.Empty;
    public string DisplayName => _displayName ?? KeyHumanReadable;
    public string Description
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_description)) return _description;
            bool turkish = SettingsManagerSettings.IsTurkish;
            string? defaultText = DefaultValue?.ToString();
            string defaultDescription = defaultText == null ? (turkish ? "belirtilmemiş" : "not set")
                : defaultText.Length == 0 ? (turkish ? "boş metin" : "empty text")
                : DefaultValue is string ? $"\"{defaultText}\"" : defaultText;
            string description = string.Format(CultureInfo.CurrentCulture, turkish ? SettingsManagerSettings.DescriptionFormatTurkish : SettingsManagerSettings.DescriptionFormatEnglish, DisplayName, TypeName, defaultDescription);
            if (!string.IsNullOrWhiteSpace(ValueExample) && ValueExample != defaultText) description += string.Format(CultureInfo.CurrentCulture, turkish ? SettingsManagerSettings.ExampleFormatTurkish : SettingsManagerSettings.ExampleFormatEnglish, ValueExample);
            if (IsOptional && AllowsNull) description += turkish ? SettingsManagerSettings.OptionalDescriptionTurkish : SettingsManagerSettings.OptionalDescriptionEnglish;
            return description;
        }
    }
    public string? ValueExample => !string.IsNullOrWhiteSpace(_valueExample) ? _valueExample : DefaultValue?.ToString();
    public bool IsOptional { get; }
    public Type ValueType => typeof(T);
    public T Value;
    public readonly T DefaultValue;
    public bool IsDefault => EqualityComparer<T>.Default.Equals(Value, DefaultValue);

    object? ISetting.Value { get => Value; set => Value = (T)value!; }
    object? ISetting.DefaultValue => DefaultValue;

    void ISettingSetup.InitializeKey(string key)
    {
        if (!string.IsNullOrEmpty(Key))
            throw new InvalidOperationException($"Key is already initialized to '{Key}'.");
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("The setting key cannot be empty.", nameof(key));
        Key = key;
        KeyHumanReadable = SettingsManager.SplitPascalCase(key);
    }

    // A rejected UI edit leaves the current value intact; a bad file value uses the default.
    public bool TrySetFromText(string text)
    {
        if (!TryParse(text, encoded: false, out T parsed)) return false;
        Value = parsed;
        return true;
    }

    public void LoadFromStr(string value)
    {
        if (TrySetFromText(value)) return;
        ResetToDefault();
        throw new FormatException($"Invalid {typeof(T).Name} for '{Key}': '{value}'. Using the default.");
    }

    public void ResetToDefault() => Value = DefaultValue;

    bool ISettingSetup.TryLoadSerialized(string text)
    {
        if (!TryParse(text, encoded: true, out T parsed)) return false;
        Value = parsed;
        return true;
    }

    private bool TryParse(string text, bool encoded, out T parsed)
    {
        parsed = default!;
        Type type = ParsedType;
        if (!IsOptional && string.IsNullOrWhiteSpace(text)) return false;

        try
        {
            object? value;
            if (type == typeof(string))
            {
                value = encoded && text == "null" ? null
                    : encoded && text.StartsWith('"') ? JsonSerializer.Deserialize<string>(text) : text;
                if (!IsOptional && string.IsNullOrWhiteSpace((string?)value)) return false;
            }
            else if (text == "null" || string.IsNullOrWhiteSpace(text))
            {
                if (!IsOptional || !AllowsNull) return false;
                value = null;
            }
            else
            {
                if (type == typeof(bool) && Int128.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out Int128 number)) text = (number != 0).ToString();
                if (type.IsEnum) text = Regex.Replace(text, @"\s+", string.Empty);
                value = type == typeof(DateTime) ? DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                    : CanReadText ? Converter.ConvertFromInvariantString(text)
                    : ParseMethod != null ? ParseMethod.Invoke(null, [text, CultureInfo.InvariantCulture])
                    : throw new NotSupportedException($"Type {type.Name} needs a TypeConverter or IParsable implementation.");
            }

            parsed = (T)value!;
            return true;
        }
        catch
        {
            return false;
        }
    }

    string ISettingSetup.Serialize()
    {
        if (Value is null) return "null";
        if (Value is string text)
        {
            // Quote only text that the original key=value format cannot preserve exactly.
            return text == "null" || text.StartsWith('"') || text != text.Trim()
                || text.Contains('\r') || text.Contains('\n')
                ? JsonSerializer.Serialize(text) : text;
        }
        if (Value is DateTime dateTime) return dateTime.ToString("O", CultureInfo.InvariantCulture);
        if (Value is DateTimeOffset offset) return offset.ToString("O", CultureInfo.InvariantCulture);
        return CanWriteText ? Converter.ConvertToInvariantString(Value) ?? string.Empty
            : Value is IFormattable formatted ? formatted.ToString(null, CultureInfo.InvariantCulture) : Value.ToString() ?? string.Empty;
    }

    public static implicit operator T(Setting<T> setting) => setting.Value;
}

internal static class SettingsManager
{
    public static string ConfigPath => Path.Combine(SettingsManagerSettings.ThisExeFolder, SettingsManagerSettings.ConfigFileName);
    public static event Action? SettingsSaved;

    private static readonly object FileLock = new();
    private static readonly Dictionary<string, ISettingSetup> iSettingSetups = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ISetting> iSettings = new(StringComparer.Ordinal);

    static SettingsManager()
    {
        foreach (FieldInfo field in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not ISettingSetup setup) continue;
            setup.InitializeKey(field.Name);
            iSettingSetups.Add(field.Name, setup);
            if (setup is ISetting setting) iSettings.Add(field.Name, setting);
        }
    }

    public static ISetting[] GetAllSettings() => [.. iSettings.Values];

    /// <summary>Loads settings, creating the file if missing. Invalid values use their defaults and produce a FormatException.</summary>
    public static void LoadSettings(string? filePath = null, bool isTurkish = false)
    {
        lock (FileLock)
        {
            SettingsManagerSettings.IsTurkish = isTurkish;
            string path = Path.GetFullPath(filePath ?? ConfigPath);
            if (!File.Exists(path)) { SaveSettings(path); return; }
            string[] lines = File.ReadAllLines(path);
            foreach (ISetting setting in iSettings.Values) setting.ResetToDefault();

            List<string> errors = [];
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith(SettingsManagerSettings.CommentPrefix, StringComparison.Ordinal)) continue;
                int separator = line.IndexOf(SettingsManagerSettings.KeyValueSeparator, StringComparison.Ordinal);
                if (separator < 0)
                {
                    errors.Add($"Invalid settings line: '{line}'.");
                    continue;
                }
                string key = line[..separator].Trim();
                if (!iSettings.TryGetValue(key, out ISetting? setting)) continue;
                string text = line[(separator + SettingsManagerSettings.KeyValueSeparator.Length)..].Trim();
                if (iSettingSetups[key].TryLoadSerialized(text)) continue;
                setting.ResetToDefault();
                errors.Add($"Invalid {setting.ValueType.Name} for '{key}': '{text}'. Using the default.");
            }
            if (errors.Count > 0) throw new FormatException(string.Join(Environment.NewLine, errors));
        }
    }

    /// <summary>Saves settings and raises SettingsSaved after a successful write. Errors propagate to the caller.</summary>
    public static void SaveSettings(string? filePath = null)
    {
        lock (FileLock)
        {
            string path = Path.GetFullPath(filePath ?? ConfigPath);
            var content = new StringBuilder($"{SettingsManagerSettings.CommentPrefix} {SettingsManagerSettings.AppTitle} Configuration{Environment.NewLine}");
            foreach (ISettingSetup setting in iSettingSetups.Values) content.AppendLine($"{setting.Key} {SettingsManagerSettings.KeyValueSeparator} {setting.Serialize()}");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tempPath, content.ToString(), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(tempPath, path, null);
                else File.Move(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
        SettingsSaved?.Invoke();
    }

    public static bool Apply(ISetting setting, string value) => setting.TrySetFromText(value);

    /// <summary>Opens an optional console settings editor. Changes are saved immediately.</summary>
    public static void OpenSettingsMenu(string? filePath = null)
    {
        ISetting[] allSettings = GetAllSettings();
        bool turkish = SettingsManagerSettings.IsTurkish;
        while (true)
        {
            Console.Clear();
            Console.WriteLine($"{SettingsManagerSettings.AppTitle} - {(turkish ? "Ayarlar" : "Settings")}{Environment.NewLine}");
            for (int i = 0; i < allSettings.Length; i++) Console.WriteLine($" [{i + 1}] {allSettings[i].DisplayName,-25}: {allSettings[i].Value?.ToString() ?? "null"}");
            Console.WriteLine(turkish ? " [0] Geri" : " [0] Back");
            Console.Write("\n> ");

            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape || key.KeyChar == '0') { SaveSettings(filePath); return; }
            if (!char.IsDigit(key.KeyChar)) continue;
            string selection = key.KeyChar.ToString();
            if (allSettings.Length > 9) { Console.Write(selection); selection += Console.ReadLine(); }
            if (!int.TryParse(selection, out int number) || number < 1 || number > allSettings.Length) continue;

            ISetting setting = allSettings[number - 1];
            if (setting.Value is bool flag) setting.Value = !flag;
            else
            {
                Console.WriteLine($"\n{setting.Description}");
                Console.Write($"{setting.DisplayName} [{setting.ValueExample}]: ");
                string? input = Console.ReadLine();
                if (input == null) { SaveSettings(filePath); return; }
                if (!setting.TrySetFromText(input))
                {
                    Console.WriteLine(turkish ? "Geçersiz değer. Devam etmek için bir tuşa basın." : "Invalid value. Press a key to continue.");
                    Console.ReadKey(true);
                    continue;
                }
            }
            SaveSettings(filePath);
        }
    }

    /// <summary>Resets in-memory values. Call SaveSettings to persist them.</summary>
    public static void ResetAllToDefaults()
    {
        foreach (ISetting setting in iSettings.Values) setting.ResetToDefault();
    }

    internal static string SplitPascalCase(string text) => Regex.Replace(text, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
}
