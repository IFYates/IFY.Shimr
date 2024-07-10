namespace IFY.Shimr;

/// <summary>
/// Marks this type as being a sourcegen shim.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class ShimgenAttribute(Type implementationType) : Attribute
{
    public Type ImplementationType { get; } = implementationType;

    // TODO: IgnoreMissingMembers logic
}

// TODO: (C# 12+) Shimgen<T>
///// <summary>
///// Marks this type as being a sourcegen shim.
///// </summary>
//[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
//public class ShimgenAttribute<T> : Attribute
//{
//    public Type ImplementationType { get; } = typeof(T);
//}