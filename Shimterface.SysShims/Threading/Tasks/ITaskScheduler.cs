using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shimterface.SysShims.Threading.Tasks
{
    /// <summary>
    /// Shim of <see cref="TaskScheduler"/>.
    /// </summary>
    public interface ITaskScheduler
    {
        int Id { get; }
        int MaximumConcurrencyLevel { get; }

        IEnumerable<ITask> GetScheduledTasks();
        bool TryExecuteTask([TypeShim(typeof(Task))] ITask task);
        bool TryExecuteTaskInline([TypeShim(typeof(Task))] ITask task, bool taskWasPreviouslyQueued);
    }
}
