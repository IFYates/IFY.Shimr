#if SHIMR_SG
using IFY.Shimr.SourceGen.CodeAnalysis;
using Microsoft.CodeAnalysis;
#endif

namespace IFY.Shimr;

/// <summary>
/// Mark a shim member as being a proxy to an implementation elsewhere.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public class ShimProxyAttribute(Type implementationType, string implementationName, ProxyBehaviour behaviour) : Attribute
{
    /// <summary>
    /// The type that implements this member.
    /// </summary>
    public Type ImplementationType { get; } = implementationType;
    /// <summary>
    /// The name of the implemenation member.
    /// </summary>
    public string? ImplementationName { get; } = implementationName;
    /// <summary>
    /// The behaviour of this proxy member.
    /// </summary>
    public ProxyBehaviour Behaviour { get; } = behaviour;

    /// <summary>
    /// Mark a shim member as being a proxy to an implementation elsewhere, using default behaviour.
    /// </summary>
    public ShimProxyAttribute(Type implementationType)
        : this(implementationType, null!, ProxyBehaviour.Default)
    {
    }
    /// <summary>
    /// Mark a shim member as being a proxy to an implementation elsewhere.
    /// </summary>
    public ShimProxyAttribute(Type implementationType, ProxyBehaviour behaviour)
        : this(implementationType, null!, behaviour)
    {
    }
    /// <summary>
    /// Mark a shim member as being an aliased proxy to an implementation elsewhere, using default behaviour.
    /// </summary>
    public ShimProxyAttribute(Type implementationType, string implementationName)
        : this(implementationType, implementationName, ProxyBehaviour.Default)
    {
    }

#if SHIMR_SG
    internal static (ITypeSymbol ImplementationType, string? ImplementationName, ProxyBehaviour Behaviour) GetArguments(AttributeData attribute)
    {
        var implementationType = (ITypeSymbol)attribute.ConstructorArguments[0].Value!;
        string? implementationName = null;
        var behaviour = ProxyBehaviour.Default;
        if (attribute.ConstructorArguments.Length == 3)
        {
            implementationName = (string?)attribute.ConstructorArguments[1].Value;
            behaviour = (ProxyBehaviour)attribute.ConstructorArguments[2].Value!;
        }
        else if (attribute.ConstructorArguments.Length == 2)
        {
            if (attribute.ConstructorArguments[1].Type!.IsType<ProxyBehaviour>())
            {
                behaviour = (ProxyBehaviour)attribute.ConstructorArguments[1].Value!;
            }
            else
            {
                implementationName = (string?)attribute.ConstructorArguments[1].Value;
            }
        }
        return (implementationType, implementationName, behaviour);
    }
#endif
}
