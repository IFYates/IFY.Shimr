using System;

namespace Shimterface
{
    /// <summary>
    /// Mark property/field or method as being static within another type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class StaticShimAttribute : Attribute
    {
        public Type TargetType { get; private set; }

        public StaticShimAttribute(Type targetType)
        {
            TargetType = targetType;
        }
    }
}
