namespace IFY.Shimr.Extensions;

[Obsolete("Use 'ShimgenAttribute' on interface and rely on generated extension methods")]
public static partial class ShimBuilder
{
    public static T Shim<T>(object obj)
    {
        throw new NotSupportedException("Use 'ShimgenAttribute' on interface and rely on generated extension methods");
        // TODO: Resolve at runtime?
    }
}
