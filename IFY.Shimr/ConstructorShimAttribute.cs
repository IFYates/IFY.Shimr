#if SHIMR_SG
using Microsoft.CodeAnalysis;
#endif

namespace IFY.Shimr;

/// <summary>
/// Mark a method as being a shim of a constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConstructorShimAttribute : StaticShimAttribute
{
    /// <summary>
    /// Mark a method as being a shim of a constructor.
    /// </summary>
    public ConstructorShimAttribute()
        : this(null!)
    {
        IsConstructor = true;
    }
    /// <summary>
    /// Mark a method as being a shim of a constructor.
    /// </summary>
    public ConstructorShimAttribute(Type targetType)
        : base(targetType)
    {
        IsConstructor = true;
    }

#if SHIMR_SG
    internal static new ITypeSymbol? GetArgument(AttributeData attribute)
    {
        return attribute.ConstructorArguments.Length == 1
            ? (ITypeSymbol)attribute.ConstructorArguments[0].Value!
            : null;
    }
#endif
}
