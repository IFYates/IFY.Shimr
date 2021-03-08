using System;

namespace Shimterface
{
    /// <summary>
    /// Mark signature type as being automatically shimmed from real implementation type
    /// </summary>
    /// <remarks>For methods and properties/fields, use <see cref="ShimAttribute"/> instead.</remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public class TypeShimAttribute : Attribute
    {
        public Type RealType { get; private set; }

        public TypeShimAttribute(Type realType)
        {
            RealType = realType;
        }
    }
}
