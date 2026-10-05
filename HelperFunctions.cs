using System.Text.RegularExpressions;

namespace DailyAnimeWallpaper;

/// <summary>Provides the two helpers required by the bundled settings and logger engines.</summary>
internal static partial class HelperFunctions
{
    internal const bool IsLogEnabled = true;

    [GeneratedRegex("([A-Z0-9])")]
    private static partial Regex CamelCaseRegex();

    public static string SplitCamelCase(string input) => CamelCaseRegex().Replace(input, " $1").Trim();
    public static void DeleteOldestFiles(string folderPath, int filesToKeep, string prefix = "")
    {
        if (!Directory.Exists(folderPath))
        {
            Log("[ERROR] [DirectoryNotFoundException] File deletion operation: Folder not found. Folder path: " + folderPath);
            return; // Klasör bulunamazsa işlemden çık
        }

        var files = Directory.GetFiles(folderPath)
                             .Select(filePath => new
                             {
                                 Path = filePath,
                                 FileName = Path.GetFileNameWithoutExtension(filePath)
                             })
                             .ToList();

        var datedFiles = new List<(string Path, DateTime Date)>();

        foreach (var file in files)
        {
            string name = file.FileName;
            name = name.Contains(prefix) ? name.Remove(name.IndexOf(prefix), prefix.Length) : name;
            name = name.Trim();
            if (DateTime.TryParseExact(name, "yyyy.MM.dd HH.mm.ss.ff",
                                       System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.None, out DateTime fileDate))
            {
                datedFiles.Add((file.Path, fileDate));
            }
            // Hatalı formatta dosya adları garanti edildiği için else bloğuna gerek yok.
        }

        var sortedFiles = datedFiles.OrderBy(f => f.Date).ToList();

        int filesToDeleteCount = sortedFiles.Count - filesToKeep;

        if (filesToDeleteCount <= 0)
        {
            return; // Silinecek dosya yok
        }

        for (int i = 0; i < filesToDeleteCount; i++)
        {
            File.Delete(sortedFiles[i].Path);
        }
    }
}
