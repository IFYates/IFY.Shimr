namespace IFY.Shimr;

/// <summary>
/// Mark a shim member as being a proxy to an implementation elsewhere.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ShimProxyAttribute class with the specified implementation type, name, and
/// proxy behavior.
/// </remarks>
/// <param name="implementationType">The type that provides the implementation to be proxied. Cannot be null.</param>
/// <param name="implementationName">The name used to identify the implementation. This value is used to distinguish between multiple implementations
/// of the same type.</param>
/// <param name="behaviour">The proxy behavior to apply. Determines how the proxy interacts with the implementation.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public sealed class ShimProxyAttribute(Type implementationType, string implementationName, ProxyBehaviour behaviour) : Attribute
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
    /// Initializes a new instance of the ShimProxyAttribute class with the specified implementation type and the
    /// default proxy behavior.
    /// </summary>
    /// <param name="implementationType">The type that provides the implementation to be proxied. Cannot be null.</param>
    public ShimProxyAttribute(Type implementationType)
        : this(implementationType, null!, ProxyBehaviour.Default)
    {
    }
    /// <summary>
    /// Initializes a new instance of the ShimProxyAttribute class with the specified implementation type and proxy
    /// behaviour.
    /// </summary>
    /// <param name="implementationType">The type that provides the implementation to be proxied. Cannot be null.</param>
    /// <param name="behaviour">The proxy behaviour to apply to the implementation.</param>
    public ShimProxyAttribute(Type implementationType, ProxyBehaviour behaviour)
        : this(implementationType, null!, behaviour)
    {
    }
    /// <summary>
    /// Initializes a new instance of the ShimProxyAttribute class with the specified implementation type and name,
    /// using the default proxy behavior.
    /// </summary>
    /// <param name="implementationType">The type that provides the implementation to be proxied. Cannot be null.</param>
    /// <param name="implementationName">The name used to identify the implementation. Cannot be null or empty.</param>
    public ShimProxyAttribute(Type implementationType, string implementationName)
        : this(implementationType, implementationName, ProxyBehaviour.Default)
    {
    }
}
