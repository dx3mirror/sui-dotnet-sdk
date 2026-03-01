namespace MystenLabs.Sui.Utils;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Task with explicit resolve/reject setters (similar to promiseWithResolvers in JS).
/// Use when you need to complete a task from outside the async flow.
/// </summary>
/// <typeparam name="T">Result type of the task.</typeparam>
public sealed class TaskWithResolvers<T>
{
    private readonly TaskCompletionSource<T> _source;

    /// <summary>
    /// The task that completes when <see cref="SetResult"/> or <see cref="SetException"/> (or <see cref="SetCanceled"/>) is called.
    /// </summary>
    public Task<T> Task => _source.Task;

    /// <summary>
    /// Creates a new task with resolvers. Call <see cref="SetResult"/> or <see cref="SetException"/> to complete.
    /// </summary>
    private TaskWithResolvers(TaskCompletionSource<T> source)
    {
        _source = source;
    }

    /// <summary>
    /// Creates a new <see cref="TaskWithResolvers{T}"/> instance.
    /// </summary>
    public static TaskWithResolvers<T> Create()
    {
        return new TaskWithResolvers<T>(new TaskCompletionSource<T>(TaskCreationOptions.None));
    }

    /// <summary>
    /// Completes the task successfully with the given value. No-op if already completed.
    /// </summary>
    public void SetResult(T value)
    {
        _source.TrySetResult(value);
    }

    /// <summary>
    /// Same as <see cref="SetResult"/> (JS-style naming).
    /// </summary>
    public void Resolve(T value)
    {
        SetResult(value);
    }

    /// <summary>
    /// Completes the task with an exception. No-op if already completed.
    /// </summary>
    public void SetException(Exception exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        _source.TrySetException(exception);
    }

    /// <summary>
    /// Same as <see cref="SetException"/> (JS-style naming).
    /// </summary>
    public void Reject(Exception exception)
    {
        SetException(exception);
    }

    /// <summary>
    /// Completes the task as canceled. No-op if already completed.
    /// </summary>
    public void SetCanceled(CancellationToken cancellationToken = default)
    {
        _source.TrySetCanceled(cancellationToken);
    }
}
