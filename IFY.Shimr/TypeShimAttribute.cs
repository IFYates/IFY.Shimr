#if SHIMR_CG
using Microsoft.CodeAnalysis;
#endif

namespace IFY.Shimr;

/// <summary>
/// Mark signature type as being automatically shimmed from real implementation type
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class TypeShimAttribute : Attribute
{
    public Type RealType { get; }

    public TypeShimAttribute(Type realType)
    {
        RealType = realType;
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
