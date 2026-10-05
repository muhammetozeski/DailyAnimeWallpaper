using System.Diagnostics;
using System.Runtime.CompilerServices;
using Polly;
using Polly.Retry;
using static DailyAnimeWallpaper.ExceptionBoundary;

namespace DailyAnimeWallpaper;

/// <summary>Centralizes retries and time budgets while preserving the final failure for the outer boundary.</summary>
internal static class Resilience
{
    const bool IsLogEnabled = true;

    /// <summary>Executes an operation whose explicitly selected failures are safe to retry.</summary>
    /// <typeparam name="T">The successful result type.</typeparam>
    /// <param name="operation">The replay-safe operation; it must honor the inner token and must not commit shared state after its wait is abandoned.</param>
    /// <param name="shouldRetry">Selects only failures that are temporary and safe to retry.</param>
    /// <param name="cancellationToken">The caller's cancellation request.</param>
    /// <param name="retryCount">The number of additional attempts; zero disables retry.</param>
    /// <param name="attemptTimeout">The per-attempt budget; defaults to fifteen seconds.</param>
    /// <param name="totalTimeout">The budget including attempts and waits; defaults to sixty seconds.</param>
    /// <param name="retryDelay">The base exponential retry delay; defaults to two hundred milliseconds.</param>
    /// <param name="operationName">The name recorded with each attempt.</param>
    /// <returns>The successful operation value; final failures propagate to the enclosing boundary.</returns>
    public static async Task<T> ExecuteResilientlyAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken = default,
        int retryCount = 2,
        TimeSpan? attemptTimeout = null,
        TimeSpan? totalTimeout = null,
        TimeSpan? retryDelay = null,
        [CallerMemberName] string operationName = "")
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(shouldRetry);
        if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount));

        var builder = new ResiliencePipelineBuilder()
            .AddTimeout(totalTimeout ?? TimeSpan.FromSeconds(60));

        if (retryCount > 0)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = retryCount,
                Delay = retryDelay ?? TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = arguments => ValueTask.FromResult(
                    arguments.Outcome.Exception is Exception exception &&
                    exception is not OperationCanceledException &&
                    shouldRetry(exception)),
                OnRetry = arguments =>
                {
                    if (IsLogEnabled)
                        LogSafely($"operation={operationName}; state=retry-scheduled; failedAttempt={arguments.AttemptNumber + 1}; delayMs={arguments.RetryDelay.TotalMilliseconds:F3}; error={arguments.Outcome.Exception}", isRun: IsLogEnabled);
                    return ValueTask.CompletedTask;
                }
            });
        }

        ResiliencePipeline pipeline = builder
            .AddTimeout(attemptTimeout ?? TimeSpan.FromSeconds(15))
            .Build();

        int attemptNumber = 0;
        Stopwatch total = Stopwatch.StartNew();
        try
        {
            return await pipeline.ExecuteAsync(async token =>
            {
                int attempt = ++attemptNumber;
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool completed = false;
                try
                {
                    if (IsLogEnabled)
                        LogSafely($"operation={operationName}; state=attempt-started; attempt={attempt}", isRun: IsLogEnabled);
                    Task<T> pending = operation(token);
                    T result;
                    try
                    {
                        result = await pending.WaitAsync(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        ObserveLateCompletion(pending, operationName, attempt);
                        throw;
                    }
                    completed = true;
                    return result;
                }
                finally
                {
                    stopwatch.Stop();
                    if (IsLogEnabled)
                        LogSafely($"operation={operationName}; state=attempt-finished; attempt={attempt}; completed={completed}; elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}", isRun: IsLogEnabled);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            total.Stop();
            if (IsLogEnabled)
                LogSafely($"operation={operationName}; state=resilience-finished; attempts={attemptNumber}; elapsedMs={total.Elapsed.TotalMilliseconds:F3}", isRun: IsLogEnabled);
        }
    }

    /// <summary>Executes a replay-safe asynchronous action with the shared retry policy.</summary>
    /// <param name="operation">The replay-safe action, which honors the inner token and cannot commit shared state after its wait is abandoned.</param>
    /// <param name="shouldRetry">Selects only failures that are temporary and safe to retry.</param>
    /// <param name="cancellationToken">The caller's cancellation request.</param>
    /// <param name="retryCount">The number of additional attempts; zero disables retry.</param>
    /// <param name="attemptTimeout">The per-attempt budget; defaults to fifteen seconds.</param>
    /// <param name="totalTimeout">The budget including attempts and waits; defaults to sixty seconds.</param>
    /// <param name="retryDelay">The base exponential retry delay; defaults to two hundred milliseconds.</param>
    /// <param name="operationName">The name recorded with each attempt.</param>
    /// <returns>A task which completes successfully or propagates the final failure.</returns>
    public static Task ExecuteResilientlyAsync(
        Func<CancellationToken, Task> operation,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken = default,
        int retryCount = 2,
        TimeSpan? attemptTimeout = null,
        TimeSpan? totalTimeout = null,
        TimeSpan? retryDelay = null,
        [CallerMemberName] string operationName = "") =>
        ExecuteResilientlyAsync(async token =>
        {
            ArgumentNullException.ThrowIfNull(operation);
            await operation(token).ConfigureAwait(false);
            return true;
        }, shouldRetry, cancellationToken, retryCount, attemptTimeout, totalTimeout, retryDelay, operationName);

    /// <summary>Observes completion of work whose caller stopped waiting, without converting it into a successful operation.</summary>
    /// <param name="pending">The task that may still be completing in isolation.</param>
    /// <param name="operationName">The operation name for the late completion record.</param>
    /// <param name="attempt">The attempt number associated with the task.</param>
    static void ObserveLateCompletion(Task pending, string operationName, int attempt)
    {
        _ = pending.ContinueWith(completed =>
        {
            Exception? exception = completed.Exception;
            if (IsLogEnabled)
                LogSafely($"operation={operationName}; state=abandoned-task-completed; attempt={attempt}; status={completed.Status}; error={exception}", isRun: IsLogEnabled);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
