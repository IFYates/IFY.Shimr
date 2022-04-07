using System;

namespace IFY.Shimr
{
    /// <summary>
    /// Mark a method as being a shim of a constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ConstructorShimAttribute : StaticShimAttribute
    {
        public ConstructorShimAttribute()
        {
            IsConstructor = true;
        }
        public ConstructorShimAttribute(Type targetType)
            : base(targetType)
        {
            IsConstructor = true;
        }
    }
}
