using System;
using System.Threading.Tasks;
using System.Threading;

namespace IFY.Shimr.Internal
{
    internal static class TaskShimHelpers
    {
        public static object? ConvertTaskResult(Task task, Type targetType)
        {
            if (task == null) return null;
            var resultProp = task.GetType().GetProperty("Result");
            if (resultProp == null) return null;
            var result = resultProp.GetValue(task);
            if (result == null) return null;
            // If targetType is assignable, just return
            if (targetType.IsAssignableFrom(result.GetType())) return result;
            // Try to shim
            return ShimBuilder.Shim(targetType, result);
        }
        public static object? ConvertValueTaskResult(object valueTask, Type targetType)
        {
            if (valueTask == null) return null;
            var resultProp = valueTask.GetType().GetProperty("Result");
            if (resultProp == null) return null;
            var result = resultProp.GetValue(valueTask);
            if (result == null) return null;
            if (targetType.IsAssignableFrom(result.GetType())) return result;
            return ShimBuilder.Shim(targetType, result);
        }
    }
}
