using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using DailyAnimeWallpaper.Localization;
using Polly.Timeout;

namespace DailyAnimeWallpaper;

/// <summary>Downloads a wallpaper, applies it on Windows, and stores files in the configured folder.</summary>
internal static class Program
{
    const bool IsLogEnabled = true;
    const string HistoricFileNamePattern = "yyyy.MM.dd.HH.mm.ss";
    const int SetDesktopWallpaper = 20;
    const int UpdateProfile = 0x01;
    const int BroadcastChange = 0x02;
    static readonly HttpClient HttpClient = new();
    static readonly string? AppName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
    static bool DeleteDownloadedFileAfterSet;

    /// <summary>Initializes shared services, runs the wallpaper operation, and returns its process status.</summary>
    /// <returns>Zero when the wallpaper was applied; otherwise one.</returns>
    static int Main()
    {
        var initialization = ApplicationServices.Initialize();
        if (!initialization.Succeeded)
        {
            Console.Error.WriteLine(Strings.StatusFailed);
            return 1;
        }

        var operation = RunSafely(() =>
        {
            CheckArgumentsFromAppName();
            string imageDirectory = Path.GetFullPath(Settings.ImageSaveDirectory.Value, AppPaths.Root);
            Log($"Wallpaper operation started; deleteAfterSet={DeleteDownloadedFileAfterSet}; imageDirectory={imageDirectory}", isRun: IsLogEnabled);
            Console.WriteLine(Strings.StatusWorking);
            bool succeeded = SetNewWallpaper(DeleteDownloadedFileAfterSet, imageDirectory);
            Console.WriteLine(succeeded ? Strings.StatusCompleted : Strings.StatusFailed);
            return succeeded;
        });

        if (!operation.Succeeded)
            Console.Error.WriteLine(Strings.StatusFailed);

        return operation.Succeeded && operation.Value == true ? 0 : 1;
    }

    /// <summary>Enables the legacy delete-after-setting mode when the executable name contains <c>$1</c>.</summary>
    static void CheckArgumentsFromAppName()
    {
        if (AppName is null)
        {
            Console.Error.WriteLine(Strings.StatusApplicationNameMissing);
            Log(Strings.StatusApplicationNameMissing, isRun: IsLogEnabled);
            return;
        }

        DeleteDownloadedFileAfterSet = AppName.Contains("$1", StringComparison.Ordinal);
    }

    /// <summary>Downloads an image, sets it as the desktop wallpaper, and optionally removes the image.</summary>
    /// <param name="deleteDownloadedFileAfterSet">Whether to remove the downloaded image after applying it.</param>
    /// <param name="imageSaveDirectory">The absolute directory used to retain the downloaded image.</param>
    /// <returns>True if Windows accepted the wallpaper change.</returns>
    static bool SetNewWallpaper(bool deleteDownloadedFileAfterSet, string imageSaveDirectory)
    {
        string? picture = DownloadNewWallpaper(ApiUrl, deleteDownloadedFileAfterSet ? null : imageSaveDirectory);
        if (picture is null)
            return false;

        if (!ChangeWindowsWallpaper(picture))
            return false;

        if (deleteDownloadedFileAfterSet)
            File.Delete(picture);

        return true;
    }

    /// <summary>Retrieves the image address from the API and saves the image to the selected folder.</summary>
    /// <param name="apiUrl">The API endpoint that returns an image URL.</param>
    /// <param name="saveFolder">The image destination, or null for the temporary application cache.</param>
    /// <returns>The downloaded image path, or null when the response has no image URL.</returns>
    static string? DownloadNewWallpaper(string apiUrl, string? saveFolder = null)
    {
        Log(string.Format(Strings.StatusDownloadingMetadata, apiUrl), isRun: IsLogEnabled);
        string json = Resilience.ExecuteResilientlyAsync(
            token => HttpClient.GetStringAsync(apiUrl, token), ShouldRetryDownload).GetAwaiter().GetResult();
        string? imageUrl = ExtractUrlFromJson(json);
        if (imageUrl is null)
        {
            Console.Error.WriteLine(Strings.StatusImageUrlMissing);
            return null;
        }

        Log(string.Format(Strings.StatusDownloadingImage, imageUrl), isRun: IsLogEnabled);
        string imageName = DateTime.Now.ToString(HistoricFileNamePattern);
        string? picturePath = DownloadFile(imageUrl, saveFolder, imageName);
        if (picturePath is not null)
        {
            Log(string.Format(Strings.StatusImageSaved, picturePath), isRun: IsLogEnabled);
            Console.WriteLine(string.Format(Strings.StatusImageSaved, picturePath));
        }

        return picturePath;
    }

    /// <summary>Returns a usable extension from an absolute image URL, defaulting to JPEG.</summary>
    /// <param name="url">The image URL.</param>
    /// <returns>The URL extension or <c>.jpg</c>.</returns>
    static string GetFileExtensionFromUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            string extension = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(extension))
                return extension;
        }

        Console.WriteLine(Strings.StatusExtensionFallback);
        return ".jpg";
    }

    /// <summary>Extracts a file name from an absolute URL or creates a unique JPEG name.</summary>
    /// <param name="url">The image URL.</param>
    /// <returns>A safe file name without a directory component.</returns>
    static string GetFileNameFromUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            string fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName;
        }

        Console.WriteLine(Strings.StatusFileNameFallback);
        return $"{Guid.NewGuid():N}.jpg";
    }

    /// <summary>Streams an image to the destination directory and returns its absolute path.</summary>
    /// <param name="url">The image URL.</param>
    /// <param name="saveFolder">The destination directory, or null to use the application cache.</param>
    /// <param name="saveName">An optional name without an extension.</param>
    /// <returns>The full downloaded image path.</returns>
    static string DownloadFile(string url, string? saveFolder = null, string? saveName = null)
    {
        string fileName = string.IsNullOrWhiteSpace(saveName)
            ? GetFileNameFromUrl(url)
            : saveName + GetFileExtensionFromUrl(url);
        string directory = saveFolder ?? AppPaths.AppCache;
        Directory.CreateDirectory(directory);
        string destination = Path.GetFullPath(Path.Combine(directory, fileName));

        byte[] imageBytes = Resilience.ExecuteResilientlyAsync(
            token => HttpClient.GetByteArrayAsync(url, token), ShouldRetryDownload).GetAwaiter().GetResult();
        File.WriteAllBytes(destination, imageBytes);
        return destination;
    }

    /// <summary>Reads the image URL property from an API JSON response.</summary>
    /// <param name="json">The response body.</param>
    /// <returns>The image URL, or null when the property is missing or empty.</returns>
    static string? ExtractUrlFromJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("items", out JsonElement items) &&
            items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0 &&
            items[0].TryGetProperty("url", out JsonElement urlProperty) &&
            urlProperty.ValueKind == JsonValueKind.String &&
            Uri.TryCreate(urlProperty.GetString(), UriKind.Absolute, out Uri? url) &&
            url.Scheme == Uri.UriSchemeHttps)
            return url.AbsoluteUri;

        return null;
    }

    /// <summary>Selects temporary transport, rate-limit, server, and request timeout failures for retry.</summary>
    /// <param name="exception">The failure from an isolated download attempt.</param>
    /// <returns>True when a repeated read request can recover from the failure.</returns>
    static bool ShouldRetryDownload(Exception exception) => exception is TimeoutRejectedException ||
        exception is HttpRequestException http &&
        (http.StatusCode is null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
         (int)http.StatusCode.Value >= 500);

    /// <summary>Sets the Windows desktop wallpaper and propagates operating-system errors.</summary>
    /// <param name="imagePath">The absolute image path.</param>
    /// <returns>True when Windows accepted the change.</returns>
    static bool ChangeWindowsWallpaper(string imagePath)
    {
        if (!SystemParametersInfo(SetDesktopWallpaper, 0, imagePath, UpdateProfile | BroadcastChange))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return true;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(int action, int parameter, string imagePath, int updateFlags);
}
