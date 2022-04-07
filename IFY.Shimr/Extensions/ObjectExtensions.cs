using System;
using System.Collections.Generic;
using System.Linq;

namespace IFY.Shimr.Extensions
{
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
        public static object CreateProxy(this Type interfaceType)
        {
            return ShimBuilder.Create(interfaceType);
        }

        #endregion CreateProxy

        #region Shim

        /// <summary>
        /// Use a shim to make the given object look like the required type.
        /// Result will also implement <see cref="IShim"/>.
        /// </summary>
        public static TInterface? Shim<TInterface>(this object? inst)
            where TInterface : class
        {
            return (TInterface?)ShimBuilder.Shim(typeof(TInterface), inst);
        }

        /// <summary>
        /// Use a shim to make the given objects look like the required type.
        /// Results will also implement <see cref="IShim"/>.
        /// </summary>
        public static IEnumerable<TInterface?>? Shim<TInterface>(this IEnumerable<object>? inst)
            where TInterface : class
        {
            return inst?.Select(i => (TInterface?)ShimBuilder.Shim(typeof(TInterface), i));
        }

        #endregion Shim

        #region Unshim

        /// <summary>
        /// Recast shim to original type.
        /// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
        /// </summary>
        public static T Unshim<T>(this object shim)
        {
            return shim is T obj ? obj : (T)((IShim)shim).Unshim();
        }

        /// <summary>
        /// Recast shims to original type.
        /// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
        /// </summary>
        public static IEnumerable<T> Unshim<T>(this IEnumerable<object> shims)
        {
            return shims.Select(Unshim<T>);
        }

        #endregion Unshim
    }
}
