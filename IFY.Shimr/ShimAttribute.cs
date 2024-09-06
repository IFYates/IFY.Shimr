#if SHIMR_SG
using Microsoft.CodeAnalysis;
#endif

namespace IFY.Shimr;

/// <summary>
/// Mark a member type as explicitly shimming an item with a different name.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public class ShimAttribute : Attribute
{
    /// <summary>
    /// The type that defines the member, for when there's a conflict.
    /// </summary>
    public Type? DefinitionType { get; }

    /// <summary>
    /// The name of the member in the target type.
    /// </summary>
    public string? ImplementationName { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="definitionType">The type that defines the member, for when there's a conflict.</param>
    public ShimAttribute(Type definitionType)
    {
        DefinitionType = definitionType;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">The name of the member in the target type.</param>
    public ShimAttribute(string name)
    {
        ImplementationName = name;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="definitionType">The type that defines the member, for when there's a conflict.</param>
    /// <param name="name">The name of the member in the target type.</param>
    public ShimAttribute(Type definitionType, string name)
    {
        DefinitionType = definitionType;
        ImplementationName = name;
    }

#if SHIMR_SG
    internal static (ITypeSymbol? DefinitionType, string? ImplementationName) GetArguments(AttributeData attribute)
    {
        ITypeSymbol? definitionType = null;
        string? implementationName = null;
        var args = attribute.ConstructorArguments;
        if (args.Length == 1)
        {
            if (args[0].Kind == TypedConstantKind.Type)
            {
                definitionType = (ITypeSymbol)args[0].Value!;
            }
            else
            {
                implementationName = (string)args[0].Value!;
            }
        }
        else if (args.Length == 2)
        {
            definitionType = (ITypeSymbol)args[0].Value!;
            implementationName = (string)args[1].Value!;
        }
        return (definitionType, implementationName);
    }
#endif
}
