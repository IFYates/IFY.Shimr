namespace IFY.Shimr;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public class ShimrAttribute : Attribute
{
    public Type TargetType { get; }
 
    public ShimrAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
