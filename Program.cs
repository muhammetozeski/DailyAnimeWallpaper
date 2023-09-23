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

        public const string HistoricFileNamePattern = @"yyyy.MM.dd.HH.mm.ss.fff";

        public const bool DeleteTheDownloadedFileAfterSet = false;

        static void Main(string[] args)
        {
            Console.WriteLine(SetNewWallpaper());
        }

        static bool SetNewWallpaper(bool deleteTheDownloadedFileAfterSet = false)
        {
            var picture = DownloadNewWallpaper(ApiUrl, SaveFolder);
            if (picture != null)
            {
                bool res = ChangeWindowsWallpaper(picture);
                if (res && deleteTheDownloadedFileAfterSet)
                    File.Delete(picture);
                return res;
            }
            else
                Console.WriteLine("Unknown picture download error");
            return false;
        }

        static string? DownloadNewWallpaper(string apiUrl, string saveFolder, string fileName = null)
        {
            if(string.IsNullOrEmpty(fileName))
                fileName = DateTime.Now.ToString(HistoricFileNamePattern);

            Console.WriteLine("Getting JSON data from: " + apiUrl);
            var json = new HttpClient().GetStringAsync(@apiUrl).Result;

            var url = ExtractUrlFromJson(json);

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
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return System.IO.Path.GetFileName(uri.LocalPath);
            }
            Console.WriteLine("File name couldn't get from server. instead using a uuid");
            return Guid.NewGuid().ToString();
        }
        static string? DownloadFile(string url, string saveFolder, string saveName = null)
        {
            string _saveName, _saveFolderFullPath;
            try
            {
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }

                _saveFolderFullPath = Path.GetFullPath(saveFolder);

                if (string.IsNullOrEmpty(saveName))
                {
                    _saveName = GetFileNameFromUrl(url);
                }
                else
                    _saveName = saveName + GetFileExtensionFromUrl(url);

                string saveTo = _saveFolderFullPath + "\\" + _saveName;
                
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
        static string ExtractUrlFromJson(string json)
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
                return false;
            }
        }

    }
}