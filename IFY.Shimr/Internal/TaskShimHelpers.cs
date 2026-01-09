using System.Reflection;
using System.Threading.Tasks;

namespace IFY.Shimr.Internal;

public static class TaskShimHelpers
{
    public static Task<T> ConvertTaskResult<T>(Task? task)
    {
        if (task == null)
        {
            return null!;
        }
        var result = task.GetType().GetProperty(nameof(Task<>.Result)).GetValue(task);
        var value = result == null ? null
            : typeof(T).IsAssignableFrom(result.GetType()) ? result
            : ShimBuilder.Shim(typeof(T), result);
        return Task.FromResult((T)value!);
    }

    public static ValueTask<T> ConvertValueTaskResult<T>(ValueTask? task)
    {
        if (task == null)
        {
            return default;
        }
        var result = task.GetType().GetProperty(nameof(ValueTask<>.Result)).GetValue(task);
        var value = result == null ? null
            : typeof(T).IsAssignableFrom(result.GetType()) ? result
            : ShimBuilder.Shim(typeof(T), result);
        return new((T)value!);
    }
}
