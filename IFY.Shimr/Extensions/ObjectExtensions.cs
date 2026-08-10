using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Extensions;

/// <summary>
/// Useful extension methods, in separate namespace to reduce pollution.
/// </summary>
public static class ObjectExtensions
{
    #region CreateProxy

    /// <summary>
    /// Create a factory proxy.
    /// Type <paramref name="interfaceType"/> must only implement methods decorated with <see cref="StaticShimAttribute"/>.
    /// </summary>
    public static object CreateProxy(this Type interfaceType) => ShimBuilder.Create(interfaceType);

    #endregion CreateProxy

    #region Shim

    /// <summary>
    /// Use a shim to make the given object look like the required type.
    /// Result will also implement <see cref="IShim"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(inst))]
    public static TInterface? Shim<TInterface>(this object? inst)
        where TInterface : class => (TInterface?)ShimBuilder.Shim(inst, typeof(TInterface));
    /// <summary>
    /// Use a shim to make the given object look like the required types.
    /// Secondary interfaces require casting to access their members.
    /// Result will also implement <see cref="IShim"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(inst))]
    public static TInterface1? Shim<TInterface1, TInterface2>(this object? inst)
        where TInterface1 : class where TInterface2 : class
        => (TInterface1?)ShimBuilder.Shim(inst, typeof(TInterface1), typeof(TInterface2));
    /// <summary>
    /// Use a shim to make the given object look like the required types.
    /// Secondary interfaces require casting to access their members.
    /// Result will also implement <see cref="IShim"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(inst))]
    public static TInterface1? Shim<TInterface1, TInterface2, TInterface3>(this object? inst)
        where TInterface1 : class where TInterface2 : class where TInterface3 : class
        => (TInterface1?)ShimBuilder.Shim(inst, typeof(TInterface1), typeof(TInterface2), typeof(TInterface3));
    /// <summary>
    /// Use a shim to make the given object look like the required types.
    /// Secondary interfaces require casting to access their members.
    /// Result will also implement <see cref="IShim"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(inst))]
    public static TInterface1? Shim<TInterface1, TInterface2, TInterface3, TInterface4>(this object? inst)
        where TInterface1 : class where TInterface2 : class where TInterface3 : class where TInterface4 : class
        => (TInterface1?)ShimBuilder.Shim(inst, typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4));
    /// <summary>
    /// Use a shim to make the given object look like the required types.
    /// Secondary interfaces require casting to access their members.
    /// Result will also implement <see cref="IShim"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(inst))]
    public static TInterface1? Shim<TInterface1, TInterface2, TInterface3, TInterface4, TInterface5>(this object? inst)
        where TInterface1 : class where TInterface2 : class where TInterface3 : class where TInterface4 : class where TInterface5 : class
        => (TInterface1?)ShimBuilder.Shim(inst, typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4), typeof(TInterface5));

    /// <summary>
    /// Use a shim to make the given objects look like the required type.
    /// Results will also implement <see cref="IShim"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(inst))]
    public static IEnumerable<TInterface?>? Shim<TInterface>(this IEnumerable<object>? inst)
        where TInterface : class => inst?.Select(i => (TInterface?)ShimBuilder.Shim(i, typeof(TInterface))).ToArray();

    #endregion Shim

    #region Unshim

    /// <summary>
    /// Recast shim to original type.
    /// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
    /// </summary>
    public static T Unshim<T>(this object shim) => (T)(shim is IShim s ? s.Unshim() : shim);

    /// <summary>
    /// Recast shims to original type.
    /// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
    /// </summary>
    public static IEnumerable<T> Unshim<T>(this IEnumerable<object> shims) => [.. shims.Select(Unshim<T>)];

    #endregion Unshim
}
