using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DailyAnimeWallpaper
{
    internal class Program
    {
        public const string ApiUrl = @"https://api.waifu.pics/sfw/waifu";

        public const string SaveFolder = "pictures";

        public const string HistoricFileNamePattern = @"yyyy.MM.dd.HH.mm.ss";

        public static bool DeleteTheDownloadedFileAfterSet = false;

        public static string? appName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName);

        static void Main()
        {
            checkArguementsFromAppName();
            Console.WriteLine(SetNewWallpaper(DeleteTheDownloadedFileAfterSet));
        }
        static void checkArguementsFromAppName()
        {
            if (appName == null)
            {
                Console.WriteLine("App name is null. Arguements couldn't fetch.");
                return;
            }
            if (appName.Contains("$1"))
            {
                DeleteTheDownloadedFileAfterSet = true;
            }
        }
        static bool SetNewWallpaper(bool deleteTheDownloadedFileAfterSet = false)
        {
            var picture = deleteTheDownloadedFileAfterSet ? 
                DownloadNewWallpaper(ApiUrl) : DownloadNewWallpaper(ApiUrl, SaveFolder);
            if (picture != null)
            {
                bool result = ChangeWindowsWallpaper(picture);
                if (result && deleteTheDownloadedFileAfterSet)
                {
                    File.Delete(picture);
                }
                return result;
            }

            Console.WriteLine("Unknown error on picture download");
            return false;
        }

        static string? DownloadNewWallpaper(string apiUrl, string? saveFolder = null, string? fileName = null)
        {
            if(string.IsNullOrEmpty(fileName))
                fileName = DateTime.Now.ToString(HistoricFileNamePattern);

            Console.WriteLine("Getting JSON data from: " + apiUrl);
            var json = new HttpClient().GetStringAsync(@apiUrl).Result;

            string? url = ExtractUrlFromJson(json);
            if(url == null)
            {
                Console.WriteLine("The image url couldn't fetch from server.\nPress any key to continue.");
                Console.ReadKey(true);
                return null;
            }
            Console.WriteLine("Downloading picture from: " + url);
            string? picturePath = DownloadFile(url, saveFolder, fileName);

            Console.WriteLine("File downloaded to: " + picturePath);

            return picturePath;
        }

        static string GetFileExtensionFromUrl(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath;
                int lastDotIndex = path.LastIndexOf(".");

                if (lastDotIndex >= 0 && lastDotIndex < path.Length - 1)
                {
                    string extension = path.Substring(lastDotIndex + 1);
                    return "." + extension;
                }

                Console.WriteLine("Couldn't get the extension. File saved as .jpg");
                return ".jpg";
            }
            catch (UriFormatException e)
            {
                Console.WriteLine(e.Message); 
                Console.WriteLine("Couldn't get the extension. File saved as .jpg");
                return ".jpg";
            }
        }
        static string GetFileNameFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri != null)
            {
                return System.IO.Path.GetFileName(uri.LocalPath);
            }
            Console.WriteLine("File name couldn't get from server. instead using a uuid");
            return Guid.NewGuid().ToString();
        }
        static string? DownloadFile(string url, string? saveFolder = null, string? saveName = null)
        {
            string? _saveName, _saveFolderFullPath = "";
            try
            {
                if (string.IsNullOrEmpty(saveName))
                {
                    _saveName = GetFileNameFromUrl(url);
                }
                else
                    _saveName = saveName + GetFileExtensionFromUrl(url);

                if (saveFolder != null)
                {
                    if (!Directory.Exists(saveFolder))
                    {
                        Directory.CreateDirectory(saveFolder);
                    }

                    _saveFolderFullPath = saveFolder + "\\";
                }
                string saveTo = Path.GetFullPath(_saveFolderFullPath + _saveName);
                
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, saveTo);
                    return saveTo;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        static string? ExtractUrlFromJson(string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("url", out var urlProperty))
                    {
                        return urlProperty.GetString();
                    }
                }
            }
            catch (JsonException e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Couldn't found url property in json");
            return null;
        }
        public static bool ChangeWindowsWallpaper(string FullPathToImage)
        {
            try
            {
                [DllImport("user32.dll", CharSet = CharSet.Auto)]
                static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

                const int SPI_SETDESKWALLPAPER = 20;
                const int SPIF_UPDATEINIFILE = 0x01;
                const int SPIF_SENDWININICHANGE = 0x02;
                SystemParametersInfo(SPI_SETDESKWALLPAPER,
                    0,
                    FullPathToImage,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("press a key to continue");
                Console.ReadKey();
                return false;
            }
        }

    }
}