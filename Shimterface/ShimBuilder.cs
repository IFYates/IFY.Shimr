using Shimterface.Standard.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shimterface
{
	/// <summary>
	/// Provides facility to create a shim that guarantees an object can be treated as the specified interface type.
	/// </summary>
	public static class ShimBuilder
	{
		/// <summary>
		/// Not needed during normal use.
		/// Clears type cache to allow multiple testing.
		/// </summary>
		public static void ResetState()
		{
			// TODO: handle shim compilation failures by removing from dynamic assembly

			_asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Shimterface.dynamic"), AssemblyBuilderAccess.Run);
			_mod = _asm.DefineDynamicModule("Shimterface.dynamic");
			//_mod = asm.DefineDynamicModule("Shimterface.dynamic", "Shimterface.dynamic.dll", true);
			_dynamicTypeCache.Clear();
			_ignoreMissingMembers.Clear();
		}

		private static readonly List<Type> _ignoreMissingMembers = new List<Type>();

		private static AssemblyBuilder _asm;
		private static ModuleBuilder _mod;
		static ShimBuilder()
		{
			ResetState();
		}

		/// <summary>
		/// Don't compile the type every time
		/// </summary>
		private static readonly Dictionary<string, Type> _dynamicTypeCache = new Dictionary<string, Type>();
		private static readonly object _sync = 1;

		#region Internal

		private static Type getShimType(Type interfaceType, Type instType)
		{
			var className = $"{instType.Name}_{instType.GetHashCode()}_{interfaceType.Name}_{interfaceType.GetHashCode()}";
			if (!_dynamicTypeCache.ContainsKey(className))
			{
				lock (_sync)
				{
					if (!_dynamicTypeCache.ContainsKey(className))
					{
						var tb = _mod.DefineType("Shim_" + className, TypeAttributes.Public
							| TypeAttributes.Class
							| TypeAttributes.AutoClass
							| TypeAttributes.AnsiClass
							| TypeAttributes.BeforeFieldInit
							| TypeAttributes.AutoLayout, null, new[] { typeof(IShim), interfaceType });

						var instField = tb.DefineField("_inst", instType, FieldAttributes.Private | FieldAttributes.InitOnly);

						tb.AddConstructor(instField);
						tb.MethodUnshim(instField);

						// Proxy all methods (including events, properties, and indexers)
						var methods = interfaceType.GetMethods()
							.Union(interfaceType.GetInterfaces().SelectMany(i => i.GetMethods())).ToArray();
						foreach (var interfaceMethod in methods)
						{
							// Must not implement unsupported attributes
							var attr = interfaceMethod.GetCustomAttribute<StaticShimAttribute>(false);
							if (attr != null)
							{
								throw new InvalidCastException("Instance shim cannot implement static member: " + interfaceType.FullName + " " + interfaceMethod.Name);
							}

							shimMember(tb, instField, instType, interfaceType, interfaceMethod);
						}

						_dynamicTypeCache.Add(className, tb.CreateType());
					}
				}
			}
			return _dynamicTypeCache[className];
		}

		private static Type getFactoryType(Type interfaceType)
		{
			var className = interfaceType.Name + "_" + interfaceType.GetHashCode();
			if (!_dynamicTypeCache.ContainsKey(className))
			{
				lock (_sync)
				{
					if (!_dynamicTypeCache.ContainsKey(className))
					{
						var tb = _mod.DefineType(className, TypeAttributes.Public
							| TypeAttributes.Class
							| TypeAttributes.AutoClass
							| TypeAttributes.AnsiClass
							| TypeAttributes.BeforeFieldInit
							| TypeAttributes.AutoLayout, null, new[] { interfaceType });

						// Proxy all methods (including events, properties, and indexers)
						foreach (var interfaceMethod in interfaceType.GetMethods())
						{
							// Must define static source
							var attr = interfaceMethod.GetAttribute<StaticShimAttribute>();
							if (attr == null)
							{
								throw new InvalidCastException("Instance shim cannot implement non-static member: " + interfaceType.FullName + " " + interfaceMethod.Name);
							}

							shimMember(tb, null, attr.TargetType, interfaceType, interfaceMethod);
						}

						_dynamicTypeCache.Add(className, tb.CreateType());
					}
				}
			}
			return _dynamicTypeCache[className];
		}

		private static void resolveMemberDetails(Type interfaceType, MethodInfo interfaceMethod, out string implMemberName, out Type implReturnType, out bool isPropertyShim)
		{
			implMemberName = interfaceMethod.Name;
			var shimReturnType = interfaceMethod.ReturnType;

			// If really a property, will need to use the PropertyInfo
			MemberInfo interfaceMember = interfaceMethod;
			isPropertyShim = interfaceMethod.IsSpecialName
				&& (interfaceMethod.Name.StartsWith("get_") || interfaceMethod.Name.StartsWith("set_"));
			if (isPropertyShim)
			{
				var propInfo = interfaceType.GetProperty(implMemberName[4..]);
				shimReturnType = propInfo.PropertyType;
				interfaceMember = propInfo;
			}

			implReturnType = shimReturnType;

			var attr = interfaceMember.GetCustomAttribute<ShimAttribute>(false);
			if (attr != null)
			{
				implReturnType = attr.ReturnType ?? implReturnType;
				implMemberName = attr.ImplementationName ?? implMemberName;
				
				if (isPropertyShim && attr.ImplementationName != null)
				{
					implMemberName = interfaceMethod.Name[0..4] + implMemberName;
				}
			}
			else
			{
				// TEMP: Obsolete
				var attr2 = interfaceMethod.GetCustomAttribute<TypeShimAttribute>(false);
				if (attr2 != null)
				{
					implReturnType = attr2.RealType ?? implReturnType;
				}
			}

			// Can only override with an interface
			if (implReturnType != shimReturnType && !shimReturnType.IsInterfaceType())
			{
				throw new NotSupportedException($"Shimmed return type ({shimReturnType.FullName}) must be an interface on member: {interfaceType.FullName}.{interfaceMethod.Name}");
			}
		}

		private static void shimMember(TypeBuilder tb, FieldBuilder instField, Type instType, Type interfaceType, MethodInfo interfaceMethod)
		{
			resolveMemberDetails(interfaceType, interfaceMethod, out var implMemberName, out var implPropType, out var isPropertyShim);
			
			// Workout real parameter types
			var paramTypes = interfaceMethod.GetParameters()
				.Select(p =>
				{
					var paramAttr = p.GetCustomAttribute<TypeShimAttribute>();
					if (paramAttr != null && !p.ParameterType.IsInterfaceType())
					{
						throw new NotSupportedException("Shimmed parameter type must be an interface: " + interfaceType.FullName);
					}
					return paramAttr == null ? p.ParameterType : paramAttr.RealType;
				}).ToArray();

			// If really a property, set arg may need to be unshimmed
			if (isPropertyShim && interfaceMethod.Name.StartsWith("set_"))
			{
				paramTypes[^1] = implPropType;
			}

			// Check if this is a property wrapping a field
			if (isPropertyShim)
			{
				var fieldInfo = instType.GetField(implMemberName[4..]);
				if (fieldInfo != null)
				{
					tb.FieldWrap(instField, interfaceMethod, fieldInfo);
					return;
				}
			}

			// Match real method
			var methodInfo = instType.GetMethod(implMemberName, paramTypes);
			if (methodInfo != null)
			{
				tb.MethodCall(instField, interfaceMethod, methodInfo);
				return;
			}

			if (_ignoreMissingMembers.Contains(interfaceType))
			{
				ILBuilder.MethodThrowException<NotImplementedException>(tb, interfaceMethod);
			}
			else
			{
				// TODO: Could support default/custom functionality
				throw new InvalidCastException($"Cannot shim {instType.FullName} as {interfaceType.FullName}; missing method: {interfaceMethod.Name}");
			}
		}

		#endregion Internal

		/// <summary>
		/// Sets the creation-time assertion that all <typeparamref name="TInterface"/> members must exist in the shimmed type.
		/// Execution of such members will throw <see cref="NotImplementedException"/>.
		/// Once set, cannot be reversed.
		/// </summary>
		public static void IgnoreMissingMembers<TInterface>()
			where TInterface : class
		{
			_ignoreMissingMembers.Add(typeof(TInterface));
		}

		#region Create

		/// <summary>
		/// Create a factory proxy.
		/// <typeparamref name="TInterface"/> must only implement methods decorated with <see cref="StaticShimAttribute"/>.
		/// </summary>
		public static TInterface Create<TInterface>()
			where TInterface : class
		{
			var factoryType = getFactoryType(typeof(TInterface));
			var factory = Activator.CreateInstance(factoryType);
			return (TInterface)factory;
		}

		#endregion Create

		#region Shim

		/// <summary>
		/// Use a shim to make the given object look like the required type.
		/// Result will also implement <see cref="IShim"/>.
		/// </summary>
		public static TInterface Shim<TInterface>(this object inst)
			where TInterface : class
		{
			return (TInterface)Shim(typeof(TInterface), inst);
		}
		/// <summary>
		/// Use a shim to make the given objects look like the required type.
		/// Results will also implement <see cref="IShim"/>.
		/// </summary>
		public static TInterface[] Shim<TInterface>(this object[] inst)
			where TInterface : class
		{
			return (TInterface[])Shim<TInterface>((IEnumerable<object>)inst);
		}
		/// <summary>
		/// Use a shim to make the given objects look like the required type.
		/// Results will also implement <see cref="IShim"/>.
		/// </summary>
		public static IEnumerable<TInterface> Shim<TInterface>(this IEnumerable<object> inst)
			where TInterface : class
		{
			return inst.Select(i => (TInterface)Shim(typeof(TInterface), i)).ToArray();
		}
		/// <summary>
		/// Use a shim to make the given object look like the required type.
		/// Result will also implement <see cref="IShim"/>.
		/// </summary>
		public static object Shim(Type interfaceType, object inst)
		{
			// Run-time test that type is an interface
			if (!interfaceType.IsInterfaceType())
			{
				throw new NotSupportedException("Generic argument must be a direct interface: " + interfaceType.FullName);
			}

			if (interfaceType.IsAssignableFrom(inst.GetType()))
			{
				return inst;
			}

			var shimType = getShimType(interfaceType, inst.GetType());
			var shim = Activator.CreateInstance(shimType, new object[] { inst });
			return shim;
		}

		#endregion Shim

		#region Unshim

		/// <summary>
		/// Recast shim to original type.
		/// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
		/// </summary>
		public static T Unshim<T>(object shim)
		{
			return shim is T obj ? obj : (T)((IShim)shim).Unshim();
		}
		/// <summary>
		/// Recast shims to original type.
		/// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
		/// </summary>
		public static T[] Unshim<T>(object[] shims)
		{
			return Unshim<T>((IEnumerable<object>)shims).ToArray();
		}
		/// <summary>
		/// Recast shims to original type.
		/// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> Unshim<T>(IEnumerable<object> shims)
		{
			return shims.Select(s => Unshim<T>(s)).ToArray();
		}

		#endregion Unshim
	}
}
