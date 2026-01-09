namespace IFY.Shimr;

/// <summary>
/// Mark signature type as being automatically shimmed from real implementation type
/// </summary>
/// <remarks>
/// Initializes a new instance of the TypeShimAttribute class with the specified real type.
/// </remarks>
/// <param name="realType">The actual type that this attribute is intended to represent. Cannot be null.</param>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class TypeShimAttribute(Type realType) : Attribute
{
    /// <summary>
    /// Gets the underlying runtime type represented by this instance.
    /// </summary>
    public Type RealType { get; } = realType;
}
