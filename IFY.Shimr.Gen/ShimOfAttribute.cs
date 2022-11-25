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
