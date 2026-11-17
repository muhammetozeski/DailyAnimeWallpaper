using System.Windows.Forms;
using DailyAnimeWallpaper.Localization;
using DailyAnimeWallpaper.Ui;

namespace DailyAnimeWallpaper;

/// <summary>Application entry point supporting one-click headless wallpaper application and the <c>settings</c> GUI.</summary>
internal static class Program
{
    const bool IsLogEnabled = true;
    static readonly string? AppName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);

    /// <summary>Initializes portable services and either opens the Settings GUI or applies a weighted random wallpaper.</summary>
    /// <param name="args">Command-line arguments; pass <c>settings</c> to open the configuration UI.</param>
    /// <returns>Zero on success; otherwise one.</returns>
    [STAThread]
    static int Main(string[] args)
    {
        var initialization = ApplicationServices.Initialize();
        if (!initialization.Succeeded)
        {
            Console.Error.WriteLine(Strings.StatusFailed);
            return 1;
        }

        if (IsSettingsModeRequested(args))
            return RunSettingsGui();

        return RunHeadlessWallpaperChange();
    }

    static bool IsSettingsModeRequested(string[] args)
    {
        foreach (string arg in args)
        {
            string normalized = arg.Trim().TrimStart('-', '/');
            if (string.Equals(normalized, SettingsArgument, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static int RunSettingsGui()
    {
        var guiOperation = RunSafely(() =>
        {
            Log("Opening Settings GUI.", isRun: IsLogEnabled);
            ApplicationConfiguration.Initialize();
            Application.Run(new SettingsForm());
        });

        if (!guiOperation.Succeeded)
            Console.Error.WriteLine(Strings.StatusFailed);

        return guiOperation.Succeeded ? 0 : 1;
    }

    static int RunHeadlessWallpaperChange()
    {
        var operation = RunSafely(() =>
        {
            bool deleteAfterSet = CheckDeleteAfterSetFromAppName();
            string imageDirectory = Path.GetFullPath(Settings.ImageSaveDirectory.Value, AppPaths.Root);
            Log($"Wallpaper operation started; deleteAfterSet={deleteAfterSet}; imageDirectory={imageDirectory}; filter={Settings.NsfwFilter.Value}", isRun: IsLogEnabled);
            Console.WriteLine(Strings.StatusWorking);

            AppliedWallpaperResult result = WallpaperEngine.ApplyWeightedRandomWallpaperAsync(
                deleteAfterSet,
                imageDirectory,
                progress: message => Console.WriteLine(message)).GetAwaiter().GetResult();

            if (!result.DeletedAfterSet)
                Console.WriteLine(string.Format(Strings.StatusImageSaved, result.SavedPath));
            Console.WriteLine(string.Format(Strings.StatusAppliedFromSource, result.ProviderName, result.SubcategoryName));
            Console.WriteLine(Strings.StatusCompleted);
            return true;
        });

        if (!operation.Succeeded)
            Console.Error.WriteLine(Strings.StatusFailed);

        return operation.Succeeded && operation.Value ? 0 : 1;
    }

    static bool CheckDeleteAfterSetFromAppName()
    {
        if (AppName is null)
        {
            Console.Error.WriteLine(Strings.StatusApplicationNameMissing);
            Log(Strings.StatusApplicationNameMissing, isRun: IsLogEnabled);
            return false;
        }

        return AppName.Contains("$1", StringComparison.Ordinal);
    }
}
