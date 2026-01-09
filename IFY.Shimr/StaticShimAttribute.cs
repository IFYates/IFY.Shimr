namespace IFY.Shimr;

/// <summary>
/// Mark individual properties/fields or methods as being static within another type, or the entire interface.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
public class StaticShimAttribute : Attribute
{
    /// <summary>
    /// The type that implements this member.
    /// </summary>
    public Type? TargetType { get; }
    /// <summary>
    /// True if this member calls a constructor on the target type.
    /// </summary>
    internal bool IsConstructor { get; set; }

    /// <summary>
    /// Initializes a new instance of the StaticShimAttribute class.
    /// </summary>
    protected internal StaticShimAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the StaticShimAttribute class.
    /// </summary>
    /// <param name="targetType">The type that this static shim targets.</param>
    public StaticShimAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
