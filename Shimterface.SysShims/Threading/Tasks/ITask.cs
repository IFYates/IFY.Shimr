using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shimterface.SysShims.Threading.Tasks
{
	/// <summary>
	/// Factory shim of <see cref="Task"/>.
	/// </summary>
	[StaticShim(typeof(Task))]
	public interface ITaskFactory
	{
		[ConstructorShim(typeof(Task))]
		ITask Create(Action action);
		[ConstructorShim(typeof(Task))]
		ITask Create(Action action, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
		[ConstructorShim(typeof(Task))]
		ITask Create(Action action, TaskCreationOptions creationOptions);
		[ConstructorShim(typeof(Task))]
		ITask Create(Action<object> action, object state);
		[ConstructorShim(typeof(Task))]
		ITask Create(Action action, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken, TaskCreationOptions creationOptions);
		[ConstructorShim(typeof(Task))]
		ITask Create(Action<object> action, object state, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
		[ConstructorShim(typeof(Task))]
		ITask Create(Action<object> action, object state, TaskCreationOptions creationOptions);
		[ConstructorShim(typeof(Task))]
		ITask Create(Action<object> action, object state, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken, TaskCreationOptions creationOptions);
	}

	/// <summary>
	/// Shim of <see cref="Task"/>.
	/// </summary>
	public interface ITask : IDisposable
	{
		TaskCreationOptions CreationOptions { get; }
		AggregateException Exception { get; }
		bool IsCompleted { get; }
		bool IsCanceled { get; }
		object AsyncState { get; }
		bool IsCompletedSuccessfully { get; }
		int Id { get; }
		bool IsFaulted { get; }
		TaskStatus Status { get; }

		ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext);
		ITask ContinueWith(Action<Task, object> continuationAction, object state);
		ITask ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken);
		ITask ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		ITask ContinueWith(Action<Task, object> continuationAction, object state, TaskContinuationOptions continuationOptions);
		ITask ContinueWith(Action<Task, object> continuationAction, object state, TaskScheduler scheduler);
		ITask ContinueWith(Action<Task> continuationAction);
		ITask ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken);
		ITask ContinueWith(Action<Task> continuationAction, TaskContinuationOptions continuationOptions);
		ITask ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken);
		ITask ContinueWith(Action<Task> continuationAction, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskContinuationOptions continuationOptions);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state);
		//ITask<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		TaskAwaiter GetAwaiter();
		void RunSynchronously();
		void RunSynchronously([TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		void Start();
		void Start([TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		void Wait(CancellationToken cancellationToken);
		bool Wait(int millisecondsTimeout);
		void Wait();
		bool Wait(int millisecondsTimeout, CancellationToken cancellationToken);
		bool Wait(TimeSpan timeout);
	}

	/// <summary>
	/// Shim of <see cref="Task&lt;&gt;"/>.
	/// </summary>
	public interface ITask<TResult> : ITask
	{
		TResult Result { get; }

		new ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext);
		ITask ContinueWith(Action<Task<TResult>, object> continuationAction, object state, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, TaskScheduler scheduler);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, TaskContinuationOptions continuationOptions);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
		ITask<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state);
		ITask ContinueWith(Action<Task<TResult>> continuationAction, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		ITask ContinueWith(Action<Task<TResult>> continuationAction, TaskContinuationOptions continuationOptions);
		ITask ContinueWith(Action<Task<TResult>> continuationAction, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		ITask ContinueWith(Action<Task<TResult>> continuationAction);
		ITask ContinueWith(Action<Task<TResult>, object> continuationAction, object state, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		ITask ContinueWith(Action<Task<TResult>, object> continuationAction, object state, TaskContinuationOptions continuationOptions);
		ITask ContinueWith(Action<Task<TResult>, object> continuationAction, object state, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken, TaskContinuationOptions continuationOptions, [TypeShim(typeof(TaskScheduler))] ITaskScheduler scheduler);
		new TaskAwaiter<TResult> GetAwaiter();
	}
}
