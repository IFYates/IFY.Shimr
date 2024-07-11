namespace IFY.Shimr.Extensions;

public static partial class ShimBuilder
{
    [Obsolete("Shimr codegen is either out-of-date or unable to provide design-time reliance on the target shim.")]
    public static T? Shim<T>(this object? obj)
    {
        throw new NotSupportedException("Rely on generated extension methods");
        // TODO: Resolve at runtime?
    }

    public static T Create<T>()
    {
        throw new NotImplementedException();
        // TODO: Resolve at runtime?
    }
}
