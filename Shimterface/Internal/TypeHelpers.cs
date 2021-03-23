using System;
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
		public static TAttribute? GetAttribute<TAttribute>(this MethodInfo methodInfo)
			where TAttribute : Attribute
		{
			var attr = methodInfo.GetCustomAttribute<TAttribute>(false);
			if (attr == null && (methodInfo.Attributes & MethodAttributes.SpecialName) > 0
					&& (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_")))
			{
				var propInfo = methodInfo.ReflectedType.GetProperty(methodInfo.Name[4..]);
				if (propInfo != null)
				{
					attr = propInfo.GetCustomAttribute<TAttribute>(false);
				}
			}
			return attr;
		}

		public static MethodInfo? GetMethod(this Type type, string name, Type[] parameterTypes, Type[] genericArgs)
		{
			if (genericArgs.Length == 0)
			{
				return type.GetMethod(name, parameterTypes);
			}

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
					var pars = m.GetParameters();
					var genArgs = m.GetGenericArguments();
					return Enumerable.Range(0, pars.Length)
						.All(i => pars[i].ParameterType.IsEquivalentGenericMethodType(parameterTypes[i]));
				}).ToArray();
			if (methods.Length > 1)
			{
				throw new AmbiguousMatchException($"Found {methods.Length} methods matching given criteria");
			}
			return methods.SingleOrDefault();
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
			if (type == other)
			{
				return true;
			}

			if (type.IsGenericMethodParameter && other.IsGenericMethodParameter)
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
					return Enumerable.Range(0, type.GenericTypeArguments.Length)
						.All(i => type.GenericTypeArguments[i].IsEquivalentGenericMethodType(other.GenericTypeArguments[i]));
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
		/// Resolves array or <see cref="IEnumerable&lt;&gt;"/> types to the internal element type, or return the given type.
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
