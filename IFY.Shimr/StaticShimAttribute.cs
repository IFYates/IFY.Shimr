#if SHIMR_CG
using Microsoft.CodeAnalysis;
#endif

namespace IFY.Shimr;

/// <summary>
/// Mark individual properties/fields or methods as being static within another type, or the entire interface.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
public class StaticShimAttribute(Type targetType) : Attribute
{
    /// <summary>
    /// The type that implements this member.
    /// </summary>
    public Type? TargetType { get; } = targetType;
    /// <summary>
    /// True if this member calls a constructor on the target type.
    /// </summary>
    public bool IsConstructor { get; internal set; }

#if SHIMR_CG
    internal static ITypeSymbol? GetArgument(AttributeData attribute)
    {
        return attribute.ConstructorArguments.Length == 1
            ? (ITypeSymbol)attribute.ConstructorArguments[0].Value!
            : null;
    }
#endif
}
