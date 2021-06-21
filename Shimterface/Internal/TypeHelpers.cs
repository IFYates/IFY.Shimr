using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shimterface.Internal
{
    internal static class TypeHelpers
    {
        public static MethodInfo BindStaticMethod(this Type type, string methodName, Type[] genericArgs, Type[] paramTypes)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == methodName && m.IsGenericMethod
                    && m.GetGenericArguments().Length == genericArgs.Length
                    && m.GetParameters().Length == paramTypes.Length
                    && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(paramTypes))
                .Single().MakeGenericMethod(genericArgs);
        }

        /// <summary>
        /// Get attribute of method, including get/set for property
        /// </summary>
        public static TAttribute? GetAttribute<TAttribute>(this MemberInfo memberInfo)
            where TAttribute : Attribute
        {
            var attr = memberInfo.GetCustomAttribute<TAttribute>(false);
            if (attr != null)
            {
                return attr;
            }

            // Might be method of property
            if (memberInfo is MethodInfo mi
                 && (mi.Attributes & MethodAttributes.SpecialName) > 0
                 && (mi.Name.StartsWith("get_") || mi.Name.StartsWith("set_")))
            {
                var propInfo = memberInfo.ReflectedType.GetProperty(memberInfo.Name[4..]);
                attr = propInfo?.GetCustomAttribute<TAttribute>(false);
            }

            return attr;
        }

        public static bool IsMatch<T, U>(this IEnumerable<T> arr, IEnumerable<U> other, Func<T, U, bool> comparer)
        {
            return Enumerable.Range(0, arr.Count()).All(i => comparer(arr.ElementAt(i), other.ElementAt(i)));
        }

        public static MethodInfo? GetMethod(this Type type, string name, Type[] parameterTypes, Type[] genericArgs)
            => GetMethod(type, name, null, parameterTypes, genericArgs);
        public static MethodInfo? GetMethod(this Type type, string name, Type? returnType, Type[] parameterTypes, Type[] genericArgs)
        {
            // Find potentials
            var methods = type.GetMethods()
                .Where(m => m.Name == name && m.GetParameters().Length == parameterTypes.Length && m.GetGenericArguments().Length == genericArgs.Length)
                .ToArray();
            if (methods.Length == 0)
            {
                return null;
            }

            // Compare parameters
            methods = methods.Where(m =>
                {
                    if (returnType != null && !m.ReturnType.IsEquivalentGenericMethodType(returnType))
                    {
                        return false;
                    }

                    var pars = m.GetParameters().Select(p => p.ParameterType).ToArray();
                    if (!pars.IsMatch(parameterTypes, (a, b) => a.IsAssignableFrom(b) || a.IsEquivalentGenericMethodType(b)))
                    {
                        return false;
                    }

                    var genArgs = m.GetGenericArguments();
                    return genArgs.IsMatch(genericArgs, (a, b) => a.IsEquivalentGenericMethodType(b));
                }).ToArray();
            if (methods.Length > 1)
            {
                throw new AmbiguousMatchException($"Found {methods.Length} methods matching given criteria");
            }
            return methods.SingleOrDefault();
        }

        /// <summary>
        /// Compares for equivalent of types in general usage.
        /// Identical, assignable, similar generic definition.
        /// </summary>
        /// <param name="type">The definition type.</param>
        /// <param name="other">The type that needs to be equivalent.</param>
        /// <returns>True if <paramref name="other"/> is equivalent to <paramref name="type"/>.</returns>
        public static bool IsEquivalentType(this Type type, Type other)
        {
            return type == other
                || type.IsAssignableFrom(other)
                || IsEquivalentGenericType(type, other);
        }

        /// <summary>
        /// Compares for equivalence of types as used in a generic method.
        /// Does not compare any part of the generic attributes.
        /// </summary>
        /// <param name="type">This type to compare</param>
        /// <param name="other">The type to compare this type against</param>
        /// <returns>True if the types can be considered equivalent</returns>
        public static bool IsEquivalentGenericMethodType(this Type type, Type other)
        {
            return type == other
                || (type.IsGenericMethodParameter && other.IsGenericMethodParameter)
                || IsEquivalentGenericType(type, other);
        }

        /// <summary>
        /// Compares for equivalent generic types and their attributes.
        /// </summary>
        /// <param name="type">The generic type to compare</param>
        /// <param name="other">The generic type to compare this type against</param>
        /// <returns>True if <paramref name="other"/> is equivalent to <paramref name="type"/>.</returns>
        public static bool IsEquivalentGenericType(this Type type, Type other)
        {
            if (type == other)
            {
                return true;
            }

            // Look for Type<?> and Type<?>
            if (type.IsGenericType && other.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();
                var genOther = other.GetGenericTypeDefinition();
                if (genType == genOther)
                {
                    // Compare type arguments
                    return type.GenericTypeArguments.IsMatch(other.GenericTypeArguments, (a, b) => a.IsEquivalentGenericMethodType(b));
                }
            }

            return false;
        }

        // Close enough estimation that we're looking at an IEnumerable<T> implementation
        private static bool isIEnumerableGeneric(Type type)
        {
            return type.IsInterface
                && type.IsGenericType
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
        }

        public static bool IsArrayType(this Type type)
        {
            return type.IsArray || isIEnumerableGeneric(type);
        }

        public static bool IsInterfaceType(this Type type)
        {
            return type.ResolveType().IsInterface;
        }

        /// <summary>
        /// Rebuilds a complex type that may make use of method generic attributes and replaces
        /// them with the actual method generic attributes of the same name.
        /// </summary>
        /// <param name="type">The type that may include method generics</param>
        /// <param name="generics">Target method generics to use</param>
        /// <returns>The rebuilt type</returns>
        public static Type RebuildGenericType(this Type type, Type[] generics)
        {
            // Method generic argument
            if (type.IsGenericMethodParameter)
            {
                return generics[type.GenericParameterPosition];
            }

            // Fixed type
            if (!type.IsGenericType)
            {
                return type;
            }

            // Rebuild using generics
            var genArgs = type.GetGenericArguments().Select(a => a.RebuildGenericType(generics)).ToArray();
            return type.GetGenericTypeDefinition().MakeGenericType(genArgs);
        }

        /// <summary>
        /// Resolves array or <see cref="IEnumerable{T}"/> types to the internal element type, or return the given type.
        /// </summary>
        /// <param name="type">A type or collection of a type.</param>
        /// <returns>A singular type.</returns>
        public static Type ResolveType(this Type type)
        {
            if (isIEnumerableGeneric(type))
            {
                return type.GenericTypeArguments[0];
            }
            return type.IsArray ? type.GetElementType() : type;
        }
    }
}
