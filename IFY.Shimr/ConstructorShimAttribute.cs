namespace IFY.Shimr;

/// <summary>
/// Mark a method as being a shim of a constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConstructorShimAttribute : StaticShimAttribute
{
    /// <summary>
    /// Initializes a new instance of the ConstructorShimAttribute class.
    /// </summary>
    public ConstructorShimAttribute()
    {
        IsConstructor = true;
    }
    /// <summary>
    /// Initializes a new instance of the ConstructorShimAttribute class.
    /// </summary>
    /// <param name="targetType">The type that this constructor shim targets.</param>
    public ConstructorShimAttribute(Type targetType)
        : base(targetType)
    {
        IsConstructor = true;
    }
}
