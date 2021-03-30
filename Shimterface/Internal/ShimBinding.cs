using System;
using System.Linq;
using System.Reflection;

namespace Shimterface.Internal
{
	internal class ShimBinding
	{
		public MethodInfo InterfaceMethod { get; }
		public MemberInfo? ImplementedMember { get; set; }
		public MemberInfo? ProxyImplementationMember { get; set; }

		public ShimBinding(MethodInfo interfaceMethod)
		{
			InterfaceMethod = interfaceMethod;
		}

		public bool Resolve(Type implType, bool isConstructor)
		{
			return resolve(implType, isConstructor, true);
		}

		private bool resolve(Type implType, bool isConstructor, bool resolveProxy)
		{
			// Workout real parameter types
			var paramTypes = InterfaceMethod.GetParameters()
				.Select(p =>
				{
					var paramAttr = p.GetCustomAttribute<TypeShimAttribute>();
					if (paramAttr != null && !p.ParameterType.IsInterfaceType())
					{
						throw new NotSupportedException($"Shimmed parameter type must be an interface: {InterfaceMethod.DeclaringType.FullName}");
					}
					return paramAttr?.RealType ?? p.ParameterType;
				}).ToArray();

			// Constructors don't provide other functionality
			if (isConstructor)
			{
				ImplementedMember = implType.GetConstructor(paramTypes);
				return true;
			}

			// If really a property, will need to get attributes from PropertyInfo
			var isPropertySetShim = InterfaceMethod.IsSpecialName && InterfaceMethod.Name.StartsWith("set_");
			var isPropertyShim = isPropertySetShim || (InterfaceMethod.IsSpecialName && InterfaceMethod.Name.StartsWith("get_"));
			MemberInfo reflectMember = InterfaceMethod;
			if (isPropertyShim)
			{
				reflectMember = InterfaceMethod.DeclaringType.GetProperty(InterfaceMethod.Name[4..]);
			}

			// Look for name override
			var bindingOptions = BindingFlags.Default;
			var implMemberName = InterfaceMethod.Name;
			var attr = reflectMember.GetAttribute<ShimAttribute>();
			if (attr?.ImplementationName != null)
			{
				implMemberName = (isPropertyShim ? implMemberName[0..4] : string.Empty)
					+ attr.ImplementationName;
			}

			// Decide if proxy
			ShimBinding? proxiedBinding = null;
			if (resolveProxy)
			{
				var proxyAttr = reflectMember.GetCustomAttribute<ShimProxyAttribute>(false);
				if (proxyAttr != null)
				{
					proxiedBinding = new ShimBinding(InterfaceMethod);
					proxiedBinding.resolve(implType, false, false);

					// Confirm behaviour is valid
					if (proxyAttr.Behaviour != ProxyBehaviour.Default)
					{
						if (proxyAttr.Behaviour == ProxyBehaviour.Add)
						{
							if (proxiedBinding.ImplementedMember != null)
							{
								throw new InvalidCastException($"Cannot proxy {implType.FullName} as {InterfaceMethod.DeclaringType.FullName}; adding existing method: {InterfaceMethod}");
							}
						}
						else if (proxiedBinding.ImplementedMember == null)
						{
							throw new InvalidCastException($"Cannot proxy {implType.FullName} as {InterfaceMethod.DeclaringType.FullName}; override of missing method: {InterfaceMethod}");
						}
					}
					
					// Apply proxy redirect
					var paramList = paramTypes.ToList();
					paramList.Insert(0, InterfaceMethod.DeclaringType);
					paramTypes = paramList.ToArray();

					bindingOptions = BindingFlags.Static | BindingFlags.Public;
					implType = proxyAttr.ImplementationType;
					implMemberName = proxyAttr.ImplementationName ?? reflectMember.Name;
				}
			}

			// Find implementation return type
			Type? implReturnType = null;
			if (isPropertyShim)
			{
				var propInfo = implType.GetProperty(implMemberName[4..]);
				implReturnType = propInfo?.PropertyType;
				if (implReturnType == null)
				{
					// Check if this is a property wrapping a field
					var fieldInfo = implType.GetField(implMemberName[4..]);
					implReturnType = fieldInfo?.FieldType;
					ImplementedMember = fieldInfo;
				}

				// Property set arg will need to be unshimmed
				if (isPropertySetShim && implReturnType != null)
				{
					paramTypes[^1] = implReturnType;
					implReturnType = null;
				}
			}

			// Find method
			if (ImplementedMember == null)
			{
				var methodInfo = implType.GetMethod(implMemberName, paramTypes, InterfaceMethod.GetGenericArguments(), bindingOptions);
				if (InterfaceMethod.IsSpecialName != methodInfo?.IsSpecialName)
				{
					methodInfo = null;
				}

				implReturnType = methodInfo?.ReturnType;
				ImplementedMember = methodInfo;
			}

			// Can only override with an interface
			if (implReturnType != null && !InterfaceMethod.ReturnType.IsEquivalentGenericMethodType(implReturnType) && !InterfaceMethod.ReturnType.IsInterfaceType())
			{
				throw new NotSupportedException($"Shimmed return type ({InterfaceMethod.ReturnType.FullName}) must be an interface, on member: {InterfaceMethod.DeclaringType.FullName}.{reflectMember.Name}");
			}

			if (proxiedBinding != null)
			{
				ProxyImplementationMember = ImplementedMember;
				ImplementedMember = proxiedBinding.ImplementedMember;
			}
			return ImplementedMember != null || ProxyImplementationMember != null;
		}
	}
}
