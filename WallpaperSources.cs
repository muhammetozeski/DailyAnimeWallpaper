using System.Text.Json;
using System.Text.Json.Serialization;

namespace DailyAnimeWallpaper;

/// <summary>Controls global content filtering across subcategories.</summary>
internal enum NsfwFilterMode
{
    UseCategorySelection = 0,
    SfwOnly = 1,
    NsfwOnly = 2
}

/// <summary>Specifies how an API endpoint response is resolved into a downloadable wallpaper image.</summary>
internal enum ProviderResponseMode
{
    JsonElementPath = 0,
    DirectUrlOrImage = 1,
    NekosMoeId = 2,
    BooruPostList = 3,
    GelbooruDapi = 4
}

/// <summary>Represents an anime wallpaper API source and its selectable subcategories.</summary>
internal sealed class WallpaperProvider
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public bool Enabled { get; set; } = true;
    public int Weight { get; set; } = DefaultWeight;
    public ProviderResponseMode ResponseMode { get; set; } = ProviderResponseMode.JsonElementPath;
    public string JsonPath { get; set; } = string.Empty;
    public string FallbackJsonPath { get; set; } = string.Empty;
    public string ImageBaseUrl { get; set; } = string.Empty;
    public List<WallpaperSubcategory> Subcategories { get; set; } = [];

    /// <summary>Creates an isolated deep copy of this provider and all of its subcategories.</summary>
    /// <returns>A cloned provider instance.</returns>
    public WallpaperProvider Clone() => new()
    {
        Id = Id,
        Name = Name,
        IsBuiltIn = IsBuiltIn,
        Enabled = Enabled,
        Weight = Weight < 0 ? 0 : Weight,
        ResponseMode = ResponseMode,
        JsonPath = JsonPath,
        FallbackJsonPath = FallbackJsonPath,
        ImageBaseUrl = ImageBaseUrl,
        Subcategories = Subcategories.Select(sub => sub.Clone()).ToList()
    };

    /// <summary>Returns subcategories that are enabled, have positive weight, and pass the active NSFW filter.</summary>
    /// <param name="filterMode">The active global NSFW filter.</param>
    /// <returns>Eligible subcategories for weighted random selection.</returns>
    public List<WallpaperSubcategory> GetEligibleSubcategories(NsfwFilterMode filterMode)
    {
        List<WallpaperSubcategory> eligible = [];
        foreach (WallpaperSubcategory sub in Subcategories)
        {
            if (!sub.Enabled || sub.Weight <= 0 || string.IsNullOrWhiteSpace(sub.Endpoint))
                continue;

            bool allowed = filterMode switch
            {
                NsfwFilterMode.SfwOnly => !sub.IsNsfw,
                NsfwFilterMode.NsfwOnly => sub.IsNsfw,
                _ => true
            };

            if (allowed)
                eligible.Add(sub);
        }

        return eligible;
    }

    public override string ToString() => $"{Name} (Enabled={Enabled}, Weight={Weight}, Subcategories={Subcategories.Count})";
}

/// <summary>Represents a selectable category or tag endpoint belonging to a <see cref="WallpaperProvider"/>.</summary>
internal sealed class WallpaperSubcategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public bool IsNsfw { get; set; }
    public bool Enabled { get; set; } = true;
    public int Weight { get; set; } = DefaultWeight;
    public int MaxRandomPage { get; set; }

    /// <summary>Creates an isolated copy of this subcategory.</summary>
    /// <returns>A cloned subcategory instance.</returns>
    public WallpaperSubcategory Clone() => new()
    {
        Id = Id,
        Name = Name,
        Endpoint = Endpoint,
        IsNsfw = IsNsfw,
        Enabled = Enabled,
        Weight = Weight < 0 ? 0 : Weight,
        MaxRandomPage = MaxRandomPage < 0 ? 0 : MaxRandomPage
    };

    public override string ToString() => $"{Name} (Enabled={Enabled}, IsNsfw={IsNsfw}, Weight={Weight})";
}

/// <summary>Root JSON payload persisted in <c>UserData/sources.json</c>.</summary>
internal sealed class SourceCatalogDocument
{
    public int SchemaVersion { get; set; } = 1;
    public List<string> RemovedBuiltInIds { get; set; } = [];
    public List<WallpaperProvider> Providers { get; set; } = [];
}

/// <summary>Loads, merges, validates, and persists the anime API source catalog under portable <c>UserData</c>.</summary>
internal static class SourceCatalogManager
{
    const bool IsLogEnabled = true;
    static readonly object SyncRoot = new();
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    static SourceCatalogDocument CurrentDocument = new();

    /// <summary>Returns a deep copy of the currently loaded provider list.</summary>
    /// <returns>Cloned providers safe for UI editing or selection.</returns>
    public static List<WallpaperProvider> GetProvidersSnapshot()
    {
        lock (SyncRoot)
        {
            return CurrentDocument.Providers.Select(provider => provider.Clone()).ToList();
        }
    }

    /// <summary>Returns a copy of removed built-in provider IDs tracked across restarts.</summary>
    /// <returns>Removed built-in provider identifiers.</returns>
    public static HashSet<string> GetRemovedBuiltInIdsSnapshot()
    {
        lock (SyncRoot)
        {
            return new HashSet<string>(CurrentDocument.RemovedBuiltInIds, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Loads <c>sources.json</c> or creates the default built-in anime API catalog when absent.</summary>
    /// <param name="filePath">Optional override path; defaults to <see cref="AppConstants.SourcesFileName"/>.</param>
    public static void LoadOrCreateCatalog(string? filePath = null)
    {
        lock (SyncRoot)
        {
            string path = Path.GetFullPath(filePath ?? SourcesFileName);
            List<WallpaperProvider> builtIns = BuiltInCatalog.CreateDefaultProviders();

            if (!File.Exists(path))
            {
                CurrentDocument = new SourceCatalogDocument
                {
                    SchemaVersion = 1,
                    RemovedBuiltInIds = [],
                    Providers = builtIns
                };
                WriteDocumentLocked(path, CurrentDocument);
                Log($"Created default anime source catalog; path={path}; providers={builtIns.Count}", isRun: IsLogEnabled);
                return;
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            SourceCatalogDocument? loaded = JsonSerializer.Deserialize<SourceCatalogDocument>(json, JsonOptions);
            if (loaded is null || loaded.Providers is null)
                throw new InvalidDataException($"Invalid source catalog file: {path}");

            NormalizeLoadedCatalog(loaded, builtIns, out bool addedNewBuiltIns);
            CurrentDocument = loaded;
            if (addedNewBuiltIns)
                WriteDocumentLocked(path, CurrentDocument);

            Log($"Loaded anime source catalog; path={path}; providers={CurrentDocument.Providers.Count}", isRun: IsLogEnabled);
        }
    }

    /// <summary>Saves an updated provider list and removed built-in tracking set atomically to disk.</summary>
    /// <param name="providers">The updated provider list.</param>
    /// <param name="removedBuiltInIds">Built-in provider IDs explicitly removed by the user.</param>
    /// <param name="filePath">Optional override path; defaults to <see cref="AppConstants.SourcesFileName"/>.</param>
    public static void SaveCatalog(
        IEnumerable<WallpaperProvider> providers,
        IEnumerable<string>? removedBuiltInIds = null,
        string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(providers);

        lock (SyncRoot)
        {
            string path = Path.GetFullPath(filePath ?? SourcesFileName);
            var document = new SourceCatalogDocument
            {
                SchemaVersion = 1,
                RemovedBuiltInIds = removedBuiltInIds?
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? [.. CurrentDocument.RemovedBuiltInIds],
                Providers = providers.Select(provider => provider.Clone()).ToList()
            };

            NormalizeProvidersInPlace(document.Providers);
            WriteDocumentLocked(path, document);
            CurrentDocument = document;
            Log($"Saved anime source catalog; path={path}; providers={CurrentDocument.Providers.Count}", isRun: IsLogEnabled);
        }
    }

    /// <summary>Rebuilds the catalog from the shipped built-in defaults while preserving nothing removed.</summary>
    /// <param name="filePath">Optional override path; defaults to <see cref="AppConstants.SourcesFileName"/>.</param>
    public static void ResetToDefaults(string? filePath = null)
    {
        List<WallpaperProvider> defaults = BuiltInCatalog.CreateDefaultProviders();
        SaveCatalog(defaults, Array.Empty<string>(), filePath);
    }

    static void NormalizeLoadedCatalog(SourceCatalogDocument document, List<WallpaperProvider> builtIns, out bool addedNewBuiltIns)
    {
        addedNewBuiltIns = false;
        document.RemovedBuiltInIds ??= [];
        NormalizeProvidersInPlace(document.Providers);

        var removedSet = new HashSet<string>(document.RemovedBuiltInIds, StringComparer.OrdinalIgnoreCase);
        var existingIds = new HashSet<string>(document.Providers.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);

        foreach (WallpaperProvider builtIn in builtIns)
        {
            if (existingIds.Contains(builtIn.Id) || removedSet.Contains(builtIn.Id))
                continue;

            document.Providers.Add(builtIn.Clone());
            addedNewBuiltIns = true;
        }
    }

    static void NormalizeProvidersInPlace(List<WallpaperProvider> providers)
    {
        foreach (WallpaperProvider provider in providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Id))
                provider.Id = $"custom-{Guid.NewGuid():N}";
            provider.Name = string.IsNullOrWhiteSpace(provider.Name) ? provider.Id : provider.Name.Trim();
            if (provider.Weight < 0)
                provider.Weight = 0;
            provider.JsonPath ??= string.Empty;
            provider.FallbackJsonPath ??= string.Empty;
            provider.ImageBaseUrl ??= string.Empty;
            provider.Subcategories ??= [];

            foreach (WallpaperSubcategory sub in provider.Subcategories)
            {
                if (string.IsNullOrWhiteSpace(sub.Id))
                    sub.Id = $"sub-{Guid.NewGuid():N}";
                sub.Name = string.IsNullOrWhiteSpace(sub.Name) ? sub.Id : sub.Name.Trim();
                sub.Endpoint = sub.Endpoint?.Trim() ?? string.Empty;
                if (sub.Weight < 0)
                    sub.Weight = 0;
                if (sub.MaxRandomPage < 0)
                    sub.MaxRandomPage = 0;
            }
        }
    }

    static void WriteDocumentLocked(string path, SourceCatalogDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string serialized = JsonSerializer.Serialize(document, JsonOptions);
        string tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, serialized, new UTF8Encoding(false));
            if (File.Exists(path))
                File.Replace(tempPath, path, null);
            else
                File.Move(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
