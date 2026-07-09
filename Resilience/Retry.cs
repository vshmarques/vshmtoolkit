using System.Runtime.ExceptionServices;

namespace VshmToolkit.Resilience;

public static class Retry
{
    public static void Execute(Action action,
                               int maxAttempts = 3,
                               Func<int, TimeSpan>? delayProvider = null,
                               Func<Exception, bool>? shouldRetry = null,
                               Action<Exception, int>? onRetry = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        ExecuteAsync(_ =>
            {
                action();
                return Task.CompletedTask;
            },
            maxAttempts,
            delayProvider,
            shouldRetry,
            onRetry)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public static T Execute<T>(Func<T> action,
                               int maxAttempts = 3,
                               Func<int, TimeSpan>? delayProvider = null,
                               Func<Exception, bool>? shouldRetry = null,
                               Action<Exception, int>? onRetry = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteAsync(_ => Task.FromResult(action()),
                                 maxAttempts,
                                 delayProvider,
                                 shouldRetry,
                                 onRetry)
                           .ConfigureAwait(false)
                           .GetAwaiter()
                           .GetResult();
    }

    public static async Task ExecuteAsync(Func<CancellationToken, Task> action,
                                          int maxAttempts = 3,
                                          Func<int, TimeSpan>? delayProvider = null,
                                          Func<Exception, bool>? shouldRetry = null,
                                          Action<Exception, int>? onRetry = null,
                                          CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object>(async ct =>
            {
                await action(ct);
                return null!;
            },
            maxAttempts,
            delayProvider,
            shouldRetry,
            onRetry,
            cancellationToken);
    }

    public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
                                                int maxAttempts = 3,
                                                Func<int, TimeSpan>? delayProvider = null,
                                                Func<Exception, bool>? shouldRetry = null,
                                                Action<Exception, int>? onRetry = null,
                                                CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        delayProvider ??= _ => TimeSpan.Zero;
        shouldRetry ??= ex => ex is not OperationCanceledException;

        List<Exception>? exceptions = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (shouldRetry(ex))
            {
                exceptions ??= [];

                exceptions.Add(ex);

                if (attempt == maxAttempts)
                    break;

                onRetry?.Invoke(ex, attempt);

                var delay = delayProvider(attempt);

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken);
            }
        }

        if (exceptions!.Count == 1)
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();

        throw new AggregateException(exceptions);
    }

    public static T Until<T>(Func<T> action,
                             Predicate<T> success,
                             int maxAttempts = 3,
                             Func<int, TimeSpan>? delayProvider = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(success);

        delayProvider ??= _ => TimeSpan.Zero;

        T result = default!;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            result = action();

            if (success(result))
                return result;

            if (attempt < maxAttempts)
            {
                var delay = delayProvider(attempt);

                if (delay > TimeSpan.Zero)
                    Thread.Sleep(delay);
            }
        }

        return result;
    }
}