using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
namespace DailyAnimeWallpaper;
public static class Logger
{
    internal const bool IsLogEnabled = true;
    /// <summary> enable or disable logging by changing this </summary>
    private static int _activateLogging = 1; // Bool yerine int kullanıyoruz (0 false, 1 true)

    public static bool ActivateLogging
    {
        get => Interlocked.CompareExchange(ref _activateLogging, 0, 0) == 1;
        set => Interlocked.Exchange(ref _activateLogging, value ? 1 : 0);
    }

    const bool PrintDebugModStyle = false;

    public static readonly string startTime = DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss.ff");

    public static readonly string LogsFolder = Path.Combine(AppPaths.AppCache, "Logs");
    public const string LogFileNamePrefix = "Logs";
    public readonly static string LogFileName = LogsFolder + "\\" + LogFileNamePrefix + " " + startTime + ".txt";

    // enter -1 to disable
    const int DeleteOlderThanLastXFile = 3;

    public static readonly ConcurrentQueue<string> AllLogs = new();

    public static readonly ConcurrentQueue<string?> AllLogsUserFriendly = new();

    private static readonly BlockingCollection<string> _logQueue = [];

    static Logger()
    {
        if (!ActivateLogging)
            return;

        if (!string.IsNullOrWhiteSpace(LogsFolder))
        {
            Directory.CreateDirectory(LogsFolder);
        }

        if (DeleteOlderThanLastXFile > -1)
            try
            {
                DeleteOldestFiles(LogsFolder, DeleteOlderThanLastXFile, LogFileNamePrefix);
            }
            catch (Exception e)
            {
                Log("An Error occured while deleting the old log files. The exception here:\n\n" + e);
            }


        new Thread(() =>
        {
            foreach (string message in _logQueue.GetConsumingEnumerable())
            {
                File.AppendAllText(LogFileName, message + "\n");
            }
        })
        { IsBackground = true }.Start();
    }


    public static string? Log(object? MessageObject, ConsoleColor? consoleColor = null, bool PrintToConsole = true, bool WriteToDisk = true, bool UseNewLine = true,
        [CallerMemberName] string callerFunction = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLine = 0, bool isRun = true
        )
    {
        if (!ActivateLogging)
        {
            if (PrintToConsole)
            {
                var defaultColor = Console.ForegroundColor;
                if (consoleColor != null)
                {
                    Console.ForegroundColor = consoleColor.Value;
                }
                if (UseNewLine)
                    Console.WriteLine(MessageObject ?? "null");
                else Console.Write(MessageObject ?? "null");
                Console.ForegroundColor = defaultColor;
            }
            return null;
        }

        if (!isRun || (!PrintToConsole && !WriteToDisk)) return null;

        string? returnValue = null;

        const string prefix = "> ";
        const string suffix = "\n-----------------------------\n\n";
        string CallerFile;
        try
        {
            CallerFile = Path.GetFileName(callerFilePath);
            if (string.IsNullOrWhiteSpace(CallerFile))
            {
                CallerFile = "null";
            }
        }
        catch (Exception e)
        {
            CallerFile = "\"exception: " + e.Message + "\"";
        }

        string now = DateTime.UtcNow.AddHours(3).ToString(); //I want Türkiye time zone style

        string Message = prefix + "[" + now + "] " +
            "[" + CallerFile + "/" + callerFunction + " Line: " + callerLine +
            " Thread Id: " + Environment.CurrentManagedThreadId + "]:\n";

        try
        {

            if (MessageObject is System.Collections.IEnumerable numerable)
            {
                foreach (var item in numerable)
                {
                    try
                    {
                        string? itemString = item?.ToString();

                        if (returnValue == null)
                            returnValue = itemString ?? "null";
                        else
                            returnValue += itemString ?? "null";

                        returnValue += "\n";
                        Message += itemString ?? "item.ToString() in given numerable, has returned null\n";
                    }
                    catch (Exception e)
                    {
                        returnValue += "error";
                        Message += "An error occured while converting to string the given object in the list in the \"Log()\" function. The error:\n";
                        Message += e;
                        Message += "\n";
                    }
                }
            }
            else
            {
                returnValue = MessageObject?.ToString();
                Message += returnValue ?? "MessageObject.ToString() has returned null";
            }

        }
        catch (Exception e)
        {
            Message += "An error occured while converting to string the given object in the \"Log()\" function. The error:\n";
            Message += e;
        }

        Message += suffix;
        if (PrintToConsole)
        {
            var defaultColor = Console.ForegroundColor;
            if (consoleColor != null)
            {
                Console.ForegroundColor = consoleColor.Value;
            }

#pragma warning disable CS0162 // Ulaşılamayan kod algılandı
            if (PrintDebugModStyle)
            {
                if (UseNewLine) Console.WriteLine(Message);
                else Console.Write(Message);
            }
            else
            {
                if (UseNewLine) Console.WriteLine(MessageObject);
                else Console.Write(MessageObject);
            }

#pragma warning restore CS0162 // Ulaşılamayan kod algılandı
            Console.ForegroundColor = defaultColor;
        }

        AllLogs.Enqueue(Message);
        AllLogsUserFriendly.Enqueue(returnValue);

        if (WriteToDisk)
            _logQueue.Add(Message);

        return returnValue;
    }
}