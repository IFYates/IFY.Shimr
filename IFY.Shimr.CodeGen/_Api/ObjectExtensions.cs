namespace IFY.Shimr.Extensions;

public static partial class ObjectExtensions
{
    //[Obsolete("Shimr codegen is either out-of-date or unable to provide design-time reliance on the target shim.")]
    public static TInterface? Shim<TInterface>(this object? obj)
        where TInterface : class
    {
        throw new NotSupportedException($"Rely on generated extension methods. Tried to shim '{obj?.GetType().FullName}' to '{typeof(TInterface).FullName}' without known registration.");
        // TODO: Resolve at runtime?
    }
}
