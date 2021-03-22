using Shimterface.Internal;
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

		private static AssemblyBuilder? _asm;
		private static ModuleBuilder? _mod;
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

		private static Type getShimType(Type interfaceType, Type implType)
		{
			var className = $"{implType.Name}_{implType.GetHashCode()}_{interfaceType.Name}_{interfaceType.GetHashCode()}";
			if (!_dynamicTypeCache.ContainsKey(className))
			{
				lock (_sync)
				{
					if (!_dynamicTypeCache.ContainsKey(className))
					{
						var tb = _mod?.DefineType("Shim_" + className, TypeAttributes.Public
							| TypeAttributes.Class
							| TypeAttributes.AutoClass
							| TypeAttributes.AnsiClass
							| TypeAttributes.BeforeFieldInit
							| TypeAttributes.AutoLayout, null, new[] { typeof(IShim), interfaceType }) ?? throw new NullReferenceException();

						var instField = tb.DefineField("_inst", implType, FieldAttributes.Private | FieldAttributes.InitOnly);

						tb.AddConstructor(instField);
						tb.AddUnshimMethod(instField);

						// Proxy all methods (including events, properties, and indexers)
						var methods = interfaceType.GetMethods()
							.Union(interfaceType.GetInterfaces().SelectMany(i => i.GetMethods()))
							.Where(m => m.IsAbstract).ToArray();
						foreach (var interfaceMethod in methods)
						{
							// Don't try to implement IShim
							if (interfaceMethod.DeclaringType == typeof(IShim))
							{
								continue;
							}

							// Must not implement unsupported attributes
							var attr = interfaceMethod.GetCustomAttribute<StaticShimAttribute>(false);
							if (attr != null)
							{
								throw new InvalidCastException("Instance shim cannot implement static member: " + interfaceType.FullName + " " + interfaceMethod.Name);
							}

							shimMember(tb, instField, implType, interfaceMethod, false);
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
						var tb = _mod?.DefineType(className, TypeAttributes.Public
							| TypeAttributes.Class
							| TypeAttributes.AutoClass
							| TypeAttributes.AnsiClass
							| TypeAttributes.BeforeFieldInit
							| TypeAttributes.AutoLayout, null, new[] { interfaceType }) ?? throw new NullReferenceException();

						// Find static source on interface
						var intAttr = interfaceType.GetCustomAttribute<StaticShimAttribute>(false);
						if (intAttr?.IsConstructor == true) // Cannot define full interface as constructor
						{
							throw new NotSupportedException("Factory interface cannot be marked as constructor shim: " + interfaceType.FullName);
						}

						// Proxy all methods (including events, properties, and indexers)
						foreach (var interfaceMethod in interfaceType.GetMethods())
						{
							// Must define static source, if not at interface
							var attr = interfaceMethod.GetAttribute<StaticShimAttribute>() ?? intAttr;
							if (attr == null)
							{
								throw new InvalidCastException("Factory shim cannot implement non-static member: " + interfaceType.FullName + " " + interfaceMethod.Name);
							}

							shimMember(tb, null, attr.TargetType ?? intAttr?.TargetType ?? typeof(void), interfaceMethod, attr.IsConstructor);
						}

						_dynamicTypeCache.Add(className, tb.CreateType());
					}
				}
			}
			return _dynamicTypeCache[className];
		}

		private static MemberInfo? resolveImplementation(Type implType, MethodInfo interfaceMethod, bool isStatic, bool isConstructor)
		{
			// Workout real parameter types
			var paramTypes = interfaceMethod.GetParameters()
				.Select(p =>
				{
					var paramAttr = p.GetCustomAttribute<TypeShimAttribute>();
					if (paramAttr != null && !p.ParameterType.IsInterfaceType())
					{
						throw new NotSupportedException("Shimmed parameter type must be an interface: " + interfaceMethod.DeclaringType.FullName);
					}
					return paramAttr?.RealType ?? p.ParameterType;
				}).ToArray();

			// Constructors don't provide other functionality
			if (isConstructor)
			{
				return implType.GetConstructor(paramTypes);
			}

			// If really a property, will need to get attributes from PropertyInfo
			var isPropertySetShim = interfaceMethod.IsSpecialName && interfaceMethod.Name.StartsWith("set_");
			var isPropertyShim = isPropertySetShim || (interfaceMethod.IsSpecialName && interfaceMethod.Name.StartsWith("get_"));
			MemberInfo reflectMember = interfaceMethod;
			if (isPropertyShim)
			{
				reflectMember = interfaceMethod.DeclaringType.GetProperty(interfaceMethod.Name[4..]);
			}

			// Look for name override
			var implMemberName = interfaceMethod.Name;
			var attr = reflectMember.GetCustomAttribute<ShimAttribute>(false);
			if (attr?.ImplementationName != null)
			{
				implMemberName = (isPropertyShim ? implMemberName[0..4] : string.Empty)
					+ attr.ImplementationName;
			}

			// Find implementation return type
			Type? implReturnType = null;
			MemberInfo? implMember = null;
			if (isPropertyShim)
			{
				var propInfo = implType.GetProperty(implMemberName[4..]);
				implReturnType = propInfo?.PropertyType;
				if (implReturnType == null)
				{
					// Check if this is a property wrapping a field
					var fieldInfo = implType.GetField(implMemberName[4..]);
					implReturnType = fieldInfo?.FieldType;
					implMember = fieldInfo;
				}

				// Property set arg will need to be unshimmed
				if (isPropertySetShim)
				{
					paramTypes[^1] = implReturnType ?? paramTypes[^1];
					implReturnType = null;
				}
			}

			// Find method
			var genArgs = interfaceMethod is MethodInfo mi ? mi.GetGenericArguments() : Array.Empty<Type>();
			if (implMember == null)
			{
				var methodInfo = implType.GetMethod(implMemberName, paramTypes, genArgs);
				implReturnType = methodInfo?.ReturnType;
				implMember = methodInfo;
			}

			// Can only override with an interface
			if (implReturnType != null && !interfaceMethod.ReturnType.IsEquivalentGenericMethodType(implReturnType) && !interfaceMethod.ReturnType.IsInterfaceType())
			{
				throw new NotSupportedException($"Shimmed return type ({interfaceMethod.ReturnType.FullName}) must be an interface, on member: {interfaceMethod.DeclaringType.FullName}.{reflectMember.Name}");
			}

			return implMember;
		}

		private static void shimMember(TypeBuilder tb, FieldBuilder? instField, Type implType, MethodInfo interfaceMethod, bool isConstructor)
		{
			// Match real member
			var memberInfo = resolveImplementation(implType, interfaceMethod, instField == null, isConstructor);
			if (memberInfo == null)
			{
				if (_ignoreMissingMembers.Contains(interfaceMethod.DeclaringType))
				{
					ILBuilder.MethodThrowException<NotImplementedException>(tb, interfaceMethod);
					return;
				}

				// TODO: Could support default/custom functionality
				throw new InvalidCastException($"Cannot shim {implType.FullName} as {interfaceMethod.DeclaringType.FullName}; missing method: {interfaceMethod}");
			}

			// Generate proxy
			switch (memberInfo)
			{
				case ConstructorInfo constrInfo:
					tb.MethodCall(interfaceMethod, constrInfo);
					return;
				case FieldInfo fieldInfo:
					tb.FieldWrap(instField, interfaceMethod, fieldInfo);
					return;
				case MethodInfo methodInfo:
					tb.MethodCall(instField, interfaceMethod, methodInfo);
					return;
				default:
					throw new InvalidOperationException();
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
		public static TInterface? Shim<TInterface>(this object inst)
			where TInterface : class
		{
			return (TInterface?)Shim(typeof(TInterface), inst);
		}
		/// <summary>
		/// Use a shim to make the given objects look like the required type.
		/// Results will also implement <see cref="IShim"/>.
		/// </summary>
		public static TInterface?[]? Shim<TInterface>(this object[] inst)
			where TInterface : class
		{
			return (TInterface?[]?)Shim<TInterface>((IEnumerable<object>)inst);
		}
		/// <summary>
		/// Use a shim to make the given objects look like the required type.
		/// Results will also implement <see cref="IShim"/>.
		/// </summary>
		public static IEnumerable<TInterface?>? Shim<TInterface>(this IEnumerable<object> inst)
			where TInterface : class
		{
			return inst?.Select(i => (TInterface?)Shim(typeof(TInterface), i)).ToArray();
		}
		/// <summary>
		/// Use a shim to make the given object look like the required type.
		/// Result will also implement <see cref="IShim"/>.
		/// </summary>
		public static object? Shim(Type interfaceType, object inst)
		{
			if (inst == null)
			{
				return null;
			}

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
