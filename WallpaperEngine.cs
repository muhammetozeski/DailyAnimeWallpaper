using DailyAnimeWallpaper.Localization;

namespace DailyAnimeWallpaper;

/// <summary>Describes a successfully downloaded and applied desktop wallpaper.</summary>
/// <param name="SavedPath">Final saved path, or temporary cache path when removed after applying.</param>
/// <param name="SourceUrl">Resolved remote image URL.</param>
/// <param name="ProviderName">API source name.</param>
/// <param name="SubcategoryName">Subcategory name.</param>
/// <param name="DeletedAfterSet">Whether the local file was deleted immediately after applying.</param>
internal sealed record AppliedWallpaperResult(
    string SavedPath,
    string SourceUrl,
    string ProviderName,
    string SubcategoryName,
    bool DeletedAfterSet);

/// <summary>Coordinates weighted CSPRNG source selection, multi-API failover, disk persistence, and desktop application.</summary>
internal static class WallpaperEngine
{
    const bool IsLogEnabled = true;
    const int MaxSubcategoryAttemptsPerProvider = 2;

    /// <summary>Selects a weighted random API and subcategory with automatic failover across all enabled providers.</summary>
    /// <param name="deleteAfterSet">Whether to store in temporary cache and delete the file after applying.</param>
    /// <param name="imageSaveDirectory">Target save folder when <paramref name="deleteAfterSet"/> is false.</param>
    /// <param name="providersOverride">Optional working provider list; uses persisted catalog when null.</param>
    /// <param name="filterOverride">Optional NSFW filter override; uses <see cref="Settings.NsfwFilter"/> when null.</param>
    /// <param name="progress">Optional progress message callback for UI status updates.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>Details of the applied wallpaper.</returns>
    public static async Task<AppliedWallpaperResult> ApplyWeightedRandomWallpaperAsync(
        bool deleteAfterSet,
        string imageSaveDirectory,
        IReadOnlyList<WallpaperProvider>? providersOverride = null,
        NsfwFilterMode? filterOverride = null,
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        NsfwFilterMode filter = filterOverride ?? Settings.NsfwFilter.Value;
        List<WallpaperProvider> sourcePool = (providersOverride ?? SourceCatalogManager.GetProvidersSnapshot())
            .Where(p => p.Enabled && p.Weight > 0 && p.GetEligibleSubcategories(filter).Count > 0)
            .Select(p => p.Clone())
            .ToList();

        if (sourcePool.Count == 0)
            throw new InvalidOperationException(Strings.ErrorNoEligibleSources);

        Log($"Starting weighted wallpaper selection; eligibleProviders={sourcePool.Count}; filter={filter}", isRun: IsLogEnabled);
        List<Exception> failures = [];

        while (sourcePool.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UInt128 totalProviderWeight = CryptoRandom.CalculateTotalWeight(sourcePool, p => p.Weight);
            WallpaperProvider provider = CryptoRandom.SelectWeighted(sourcePool, p => p.Weight);
            sourcePool.Remove(provider);

            List<WallpaperSubcategory> subPool = provider.GetEligibleSubcategories(filter);
            int subAttempts = Math.Min(MaxSubcategoryAttemptsPerProvider, subPool.Count);

            for (int attempt = 0; attempt < subAttempts && subPool.Count > 0; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WallpaperSubcategory subcategory = CryptoRandom.SelectWeighted(subPool, s => s.Weight);
                subPool.Remove(subcategory);

                try
                {
                    string statusMsg = string.Format(Strings.StatusTryingSource, provider.Name, subcategory.Name);
                    progress?.Invoke(statusMsg);
                    Log($"Selected candidate; provider={provider.Name} (weight={provider.Weight}/{totalProviderWeight}); subcategory={subcategory.Name} (weight={subcategory.Weight})", isRun: IsLogEnabled);

                    return await DownloadAndSetCoreAsync(provider, subcategory, deleteAfterSet, imageSaveDirectory, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failures.Add(new InvalidOperationException($"[{provider.Name} / {subcategory.Name}] {ex.Message}", ex));
                    Log($"Source attempt failed; provider={provider.Name}; subcategory={subcategory.Name}; remainingProviders={sourcePool.Count}; error={ex}", isRun: IsLogEnabled);
                }
            }
        }

        throw new AggregateException(Strings.ErrorAllSourcesFailed, failures);
    }

    /// <summary>Downloads and sets a wallpaper from a single explicitly chosen provider and optional subcategory.</summary>
    /// <param name="provider">The provider to test.</param>
    /// <param name="subcategory">Specific subcategory, or null to pick a weighted subcategory from <paramref name="provider"/>.</param>
    /// <param name="deleteAfterSet">Whether to delete the downloaded file after setting.</param>
    /// <param name="imageSaveDirectory">Target directory for keeping downloaded images.</param>
    /// <param name="filterOverride">Optional NSFW filter mode when choosing a subcategory.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>Details of the applied wallpaper.</returns>
    public static async Task<AppliedWallpaperResult> ApplyFromSpecificSourceAsync(
        WallpaperProvider provider,
        WallpaperSubcategory? subcategory,
        bool deleteAfterSet,
        string imageSaveDirectory,
        NsfwFilterMode? filterOverride = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        WallpaperSubcategory chosenSub = subcategory ?? SelectSingleSubcategory(provider, filterOverride ?? Settings.NsfwFilter.Value);
        return await DownloadAndSetCoreAsync(provider, chosenSub, deleteAfterSet, imageSaveDirectory, cancellationToken).ConfigureAwait(false);
    }

    static WallpaperSubcategory SelectSingleSubcategory(WallpaperProvider provider, NsfwFilterMode filter)
    {
        List<WallpaperSubcategory> eligible = provider.GetEligibleSubcategories(filter);
        if (eligible.Count == 0)
            throw new InvalidOperationException(Strings.ErrorNoEligibleSources);

        return CryptoRandom.SelectWeighted(eligible, s => s.Weight);
    }

    static async Task<AppliedWallpaperResult> DownloadAndSetCoreAsync(
        WallpaperProvider provider,
        WallpaperSubcategory subcategory,
        bool deleteAfterSet,
        string imageSaveDirectory,
        CancellationToken cancellationToken)
    {
        DownloadedWallpaper downloaded = await WallpaperClient.FetchWallpaperAsync(provider, subcategory, cancellationToken).ConfigureAwait(false);

        string targetDirectory = deleteAfterSet ? AppPaths.AppCache : imageSaveDirectory;
        Directory.CreateDirectory(targetDirectory);

        string baseName = DateTime.Now.ToString(HistoricFileNamePattern);
        string destinationPath = BuildUniqueFilePath(targetDirectory, baseName, downloaded.Extension);
        await File.WriteAllBytesAsync(destinationPath, downloaded.Bytes, cancellationToken).ConfigureAwait(false);

        Log($"Saved wallpaper image; path={destinationPath}; bytes={downloaded.Bytes.Length}", isRun: IsLogEnabled);

        try
        {
            WallpaperSetter.SetDesktopWallpaper(destinationPath);
        }
        catch
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);
            throw;
        }

        if (deleteAfterSet && File.Exists(destinationPath))
            File.Delete(destinationPath);

        return new AppliedWallpaperResult(
            destinationPath,
            downloaded.SourceUrl,
            downloaded.ProviderName,
            downloaded.SubcategoryName,
            deleteAfterSet);
    }

    static string BuildUniqueFilePath(string directory, string baseName, string extension)
    {
        string candidate = Path.GetFullPath(Path.Combine(directory, baseName + extension));
        if (!File.Exists(candidate))
            return candidate;

        int suffix = 1;
        while (true)
        {
            string next = Path.GetFullPath(Path.Combine(directory, $"{baseName}_{suffix}{extension}"));
            if (!File.Exists(next))
                return next;
            suffix++;
        }
    }
}
