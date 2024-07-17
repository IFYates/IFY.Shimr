#if SHIMR_CG
using Microsoft.CodeAnalysis;
#endif

namespace IFY.Shimr;

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

#if SHIMR_CG
    public static ITypeSymbol? GetArgument(AttributeData attribute)
    {
        return attribute.ConstructorArguments.Length == 1
            ? (ITypeSymbol)attribute.ConstructorArguments[0].Value!
            : null;
    }
#endif
}
