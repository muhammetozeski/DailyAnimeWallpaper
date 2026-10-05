using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DailyAnimeWallpaper;

/// <summary>Describes the outcome without treating a failed operation as successful.</summary>
internal enum OperationStatus
{
    Failed,
    Succeeded,
    Canceled
}

/// <summary>Contains the value or the failure reported by the final exception boundary.</summary>
/// <typeparam name="T">The successful result type.</typeparam>
internal readonly record struct OperationResult<T>(OperationStatus Status, T? Value, Exception? Error)
{
    const bool IsLogEnabled = true;
    /// <summary>Gets whether the operation completed successfully.</summary>
    public bool Succeeded => Status == OperationStatus.Succeeded;
}

/// <summary>
/// Owns final exception containment after the operation has exhausted its recovery paths.
/// Internal services must propagate failures to this boundary instead of nesting boundaries.
/// </summary>
internal static class ExceptionBoundary
{
    const bool IsLogEnabled = true;
    static readonly object EmergencyLogLock = new();

    /// <summary>Runs an operation and logs and contains its final failure.</summary>
    /// <typeparam name="T">The successful result type.</typeparam>
    /// <param name="operation">The operation, including its recovery paths.</param>
    /// <param name="cancellationToken">The caller's cooperative cancellation request.</param>
    /// <param name="operationName">The name recorded with the outcome.</param>
    /// <returns>A successful value, failure, or cancellation.</returns>
    public static OperationResult<T> RunSafely<T>(
        Func<T> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string operationName = "")
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string operationId = Guid.NewGuid().ToString("N");
        OperationStatus status = OperationStatus.Failed;
        LogSafely($"operation={operationName}; id={operationId}; state=started", isRun: IsLogEnabled);

        try
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();
            T value = operation();
            status = OperationStatus.Succeeded;
            return new(OperationStatus.Succeeded, value, null);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            status = OperationStatus.Canceled;
            LogSafely($"operation={operationName}; id={operationId}; state=canceled; error={exception}", isRun: IsLogEnabled);
            return new(OperationStatus.Canceled, default, exception);
        }
        catch (Exception exception)
        {
            LogSafely($"operation={operationName}; id={operationId}; state=failed; error={exception}", isRun: IsLogEnabled);
            return new(OperationStatus.Failed, default, exception);
        }
        finally
        {
            stopwatch.Stop();
            LogSafely($"operation={operationName}; id={operationId}; state=finished; outcome={status}; elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}", isRun: IsLogEnabled);
        }
    }

    /// <summary>Runs an action with the same final failure boundary as value-returning operations.</summary>
    /// <param name="operation">The action, including its recovery paths.</param>
    /// <param name="cancellationToken">The caller's cooperative cancellation request.</param>
    /// <param name="operationName">The name recorded with the outcome.</param>
    /// <returns>A true successful value, failure, or cancellation.</returns>
    public static OperationResult<bool> RunSafely(
        Action operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string operationName = "") =>
        RunSafely(() =>
        {
            ArgumentNullException.ThrowIfNull(operation);
            operation();
            return true;
        }, cancellationToken, operationName);

    /// <summary>Awaits the entire operation and logs and contains its final failure.</summary>
    /// <typeparam name="T">The successful result type.</typeparam>
    /// <param name="operation">The asynchronous operation, including all recovery paths.</param>
    /// <param name="cancellationToken">The caller's cooperative cancellation request.</param>
    /// <param name="operationName">The name recorded with the outcome.</param>
    /// <returns>A successful value, failure, or cancellation.</returns>
    public static async Task<OperationResult<T>> RunSafelyAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string operationName = "")
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string operationId = Guid.NewGuid().ToString("N");
        OperationStatus status = OperationStatus.Failed;
        LogSafely($"operation={operationName}; id={operationId}; state=started", isRun: IsLogEnabled);

        try
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();
            T value = await operation(cancellationToken).ConfigureAwait(false);
            status = OperationStatus.Succeeded;
            return new(OperationStatus.Succeeded, value, null);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            status = OperationStatus.Canceled;
            LogSafely($"operation={operationName}; id={operationId}; state=canceled; error={exception}", isRun: IsLogEnabled);
            return new(OperationStatus.Canceled, default, exception);
        }
        catch (Exception exception)
        {
            LogSafely($"operation={operationName}; id={operationId}; state=failed; error={exception}", isRun: IsLogEnabled);
            return new(OperationStatus.Failed, default, exception);
        }
        finally
        {
            stopwatch.Stop();
            LogSafely($"operation={operationName}; id={operationId}; state=finished; outcome={status}; elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}", isRun: IsLogEnabled);
        }
    }

    /// <summary>Awaits an asynchronous action through the common final failure boundary.</summary>
    /// <param name="operation">The asynchronous action, including all recovery paths.</param>
    /// <param name="cancellationToken">The caller's cooperative cancellation request.</param>
    /// <param name="operationName">The name recorded with the outcome.</param>
    /// <returns>A true successful value, failure, or cancellation.</returns>
    public static Task<OperationResult<bool>> RunSafelyAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string operationName = "") =>
        RunSafelyAsync(async token =>
        {
            ArgumentNullException.ThrowIfNull(operation);
            await operation(token).ConfigureAwait(false);
            return true;
        }, cancellationToken, operationName);

    /// <summary>Prevents logging failures from escaping the final application boundary.</summary>
    /// <param name="message">The record to deliver to the logger or emergency local sink.</param>
    /// <param name="isRun">The originating class logging switch.</param>
    public static void LogSafely(string message, bool isRun = true)
    {
        if (isRun)
        {
            try
            {
                Log(message, isRun: isRun);
            }
            catch (Exception loggingException)
            {
                string emergencyMessage = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}Logging failure: {loggingException}{Environment.NewLine}";

                try
                {
                    lock (EmergencyLogLock)
                    {
                        Directory.CreateDirectory(AppPaths.AppCache);
                        File.AppendAllText(Path.Combine(AppPaths.AppCache, "ExceptionBoundary.Emergency.log"), emergencyMessage);
                    }
                }
                catch (Exception emergencyException)
                {
                    try
                    {
                        Console.Error.WriteLine($"{emergencyMessage}Emergency file failure: {emergencyException}");
                    }
                    catch
                    {
                        // All logging sinks failed; this boundary still contains the original failure.
                    }
                }
            }
        }
    }
}
