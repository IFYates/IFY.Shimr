using System;

namespace IFY.Shimr
{
    /// <summary>
    /// Mark individual properties/fields or methods as being static within another type, or the entire interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class StaticShimAttribute : Attribute
    {
        /// <summary>
        /// The type that implements this member.
        /// </summary>
        public Type? TargetType { get; }
        /// <summary>
        /// True if this member calls a constructor on the target type.
        /// </summary>
        internal bool IsConstructor { get; set; }

        protected StaticShimAttribute()
        {
        }

        public StaticShimAttribute(Type targetType)
        {
            TargetType = targetType;
        }
    }
}
