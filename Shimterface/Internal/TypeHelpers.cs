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
						.All(i => pars[i].ParameterType.IsEquivalentTo(parameterTypes[i], genArgs, genericArgs));
				}).ToArray();
			return methods.SingleOrDefault();
		}

		/// <summary>
		/// Compares for equivalence of generic types.
		/// Does not consider the generic order in equivalence test.
		/// </summary>
		/// <param name="type">This type to compare</param>
		/// <param name="other">The type to compare this type against</param>
		/// <param name="typeGenerics">The generic types that are accessible to this type</param>
		/// <param name="otherGenerics">The generic types that are accessible to the other type</param>
		/// <returns>True if the types can be considered equivalent</returns>
		public static bool IsEquivalentTo(this Type type, Type other, Type[] typeGenerics, Type[] otherGenerics)
		{
			if (type == other)
			{
				return true;
			}

			if (typeGenerics.Contains(type) && otherGenerics.Contains(other))
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
					return Enumerable.Range(0, genType.GenericTypeArguments.Length)
						.All(i => genType.GenericTypeArguments[i].IsEquivalentTo(genOther.GenericTypeArguments[i], typeGenerics, otherGenerics));
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

		public static Type ResolveGenericType(Type type, Type[] generics)
		{
			if (generics.Contains(type))
			{
				return type;
			}

			return null;
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
