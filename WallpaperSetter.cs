using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DailyAnimeWallpaper;

/// <summary>Applies downloaded image files as the Windows desktop wallpaper via Win32 APIs.</summary>
internal static class WallpaperSetter
{
    const bool IsLogEnabled = true;

    /// <summary>Sets the specified local image file as the desktop wallpaper and broadcasts the profile update.</summary>
    /// <param name="imagePath">The absolute path of the image file on disk.</param>
    public static unsafe void SetDesktopWallpaper(string imagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        string fullPath = Path.GetFullPath(imagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Wallpaper image file does not exist.", fullPath);

        fixed (char* pathPtr = fullPath)
        {
            var applied = PInvoke.SystemParametersInfo(
                SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDESKWALLPAPER,
                0,
                pathPtr,
                SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_UPDATEINIFILE | SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_SENDCHANGE);

            if (!applied)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"SystemParametersInfo(SPI_SETDESKWALLPAPER) failed for '{fullPath}' (Win32Error={error}).");
            }
        }

        Log($"Applied Windows desktop wallpaper; path={fullPath}", isRun: IsLogEnabled);
    }
}
