namespace IFY.Shimr;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public class ShimOfAttribute : Attribute
{
    public Type TargetType { get; }

    public ShimOfAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public class ShimOfAttribute<T> : ShimOfAttribute
{
    public ShimOfAttribute()
        : base(typeof(T))
    { }
}
