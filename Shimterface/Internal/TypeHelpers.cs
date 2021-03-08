using System;
using System.Linq;
using System.Reflection;

namespace Shimterface
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
        public static TAttribute GetAttribute<TAttribute>(this MethodInfo methodInfo)
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

        public static bool IsArrayType(this Type type)
        {
            // TODO: Better way to determine is underived IEnumerable<?>
            return type.IsArray
                || type.Namespace + "." + type.Name == "System.Collections.Generic.IEnumerable`1";
        }

        public static bool IsInterfaceType(this Type type)
        {
            return type.ResolveType().IsInterface;
        }

        public static Type ResolveType(this Type type)
        {
            // TODO: Better way to determine is underived IEnumerable<?>
            if (type.Namespace + "." + type.Name == "System.Collections.Generic.IEnumerable`1")
            {
                return type.GenericTypeArguments[0];
            }
            return type.IsArray ? type.GetElementType() : type;
        }
    }
}
