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
    /// Initializes a new instance of the <see cref="ShimAttribute"/> class using the specified definition type.
    /// </summary>
    /// <remarks>Use this constructor to associate a shim with a specific type, enabling customized behavior
    /// at runtime. Passing a null value for <paramref name="definitionType"/> will result in an exception.</remarks>
    /// <param name="definitionType">The type that defines the member, for when there's a conflict.</param>
    public ShimAttribute(Type definitionType)
    {
        DefinitionType = definitionType;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ShimAttribute"/> class using the specified member name.
    /// </summary>
    /// <param name="name">The name of the member in the target type.</param>
    public ShimAttribute(string name)
    {
        ImplementationName = name;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ShimAttribute"/> class using the specified definition type and member name.
    /// </summary>
    /// <param name="definitionType">The type that defines the member, for when there's a conflict.</param>
    /// <param name="name">The name of the member in the target type.</param>
    public ShimAttribute(Type definitionType, string name)
    {
        DefinitionType = definitionType;
        ImplementationName = name;
    }
}
