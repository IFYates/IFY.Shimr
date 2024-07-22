#if SHIMR_CG
using Microsoft.CodeAnalysis;
#endif

namespace IFY.Shimr;

/// <summary>
/// Mark signature type as being automatically shimmed from real implementation type
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class TypeShimAttribute(Type realType) : Attribute
{
    /// <summary>
    /// The underlying type of the parameter.
    /// </summary>
    public Type RealType { get; } = realType;

#if SHIMR_CG
    internal static ITypeSymbol? GetArgument(AttributeData attribute)
    {
        return attribute.ConstructorArguments.Length == 1
            ? (ITypeSymbol)attribute.ConstructorArguments[0].Value!
            : null;
    }
#endif
}
