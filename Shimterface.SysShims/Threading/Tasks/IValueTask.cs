using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Shimterface.SysShims.Threading.Tasks
{
    /// <summary>
    /// Shim of <see cref="ValueTask"/>.
    /// </summary>
    public interface IValueTask : IEquatable<ValueTask>
    {
        bool IsCanceled { get; }
        bool IsCompleted { get; }
        bool IsCompletedSuccessfully { get; }
        bool IsFaulted { get; }

        ITask AsTask();
        ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext);
        bool Equals(object obj);
        bool Equals([TypeShim(typeof(ValueTask))] IValueTask other);
        ValueTaskAwaiter GetAwaiter();
        int GetHashCode();
        IValueTask Preserve();
    }
    
    /// <summary>
    /// Shim of <see cref="ValueTask&lt;&gt;"/>.
    /// </summary>
    public interface IValueTask<TResult> : IEquatable<ValueTask<TResult>>
    {
        bool IsCanceled { get; }
        bool IsCompleted { get; }
        bool IsCompletedSuccessfully { get; }
        bool IsFaulted { get; }

        ITask<TResult> AsTask();
        ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext);
        bool Equals(object obj);
        bool Equals([TypeShim(typeof(ValueTask<>))] IValueTask<TResult> other);
        ValueTaskAwaiter<TResult> GetAwaiter();
        int GetHashCode();
        IValueTask<TResult> Preserve();
    }
}