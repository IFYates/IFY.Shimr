using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shimterface.Internal
{
	internal static class ILBuilder
	{
		private static bool resolveIfInstance(bool isStatic, ILGenerator impl, FieldInfo? instField)
		{
			if (isStatic || instField == null)
			{
				return false;
			}

			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(instField.FieldType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, instField);
			return true;
		}
		private static void resolveParameters(ILGenerator impl, MethodBase methodInfo, MethodInfo interfaceMethod, bool isProxy = false)
		{
			// Pass each parameter from the method call to the implementation
			var pars1 = methodInfo.GetParameters();
			var pars2 = interfaceMethod.GetParameters();
			for (var i = 0; i < pars1.Length; ++i)
			{
				// Proxies take "this" as first arg
				if (isProxy)
				{
					impl.Emit(OpCodes.Ldarg_0); // this
				}
				else
				{
					impl.Emit(OpCodes.Ldarg, i + 1);
					impl.EmitTypeUnshim(pars2[i].ParameterType, pars1[i].ParameterType);
				}
			}
		}

		public static void AddConstructor(this TypeBuilder tb, FieldInfo instField)
		{
			// .constr(object inst)
			var constr = tb.DefineConstructor(MethodAttributes.Public
				| MethodAttributes.HideBySig
				| MethodAttributes.SpecialName
				| MethodAttributes.RTSpecialName,
				CallingConventions.Standard, new[] { instField.FieldType });
			constr.DefineParameter(1, ParameterAttributes.None, "inst");
			var impl = constr.GetILGenerator();

			// Call to base()
			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

			// Set this._inst to the parameter
			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(OpCodes.Ldarg_1); // inst
			impl.Emit(OpCodes.Stfld, instField);

			impl.Emit(OpCodes.Ret);
		}

		public static void AddUnshimMethod(this TypeBuilder tb, FieldInfo instField)
		{
			// object Unshim()
			var impl = tb.DefinePublicMethod("Unshim", typeof(object));
			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(OpCodes.Ldfld, instField);
			if (instField.FieldType.IsValueType)
			{
				impl.Emit(OpCodes.Box, instField.FieldType);
			}
			impl.Emit(OpCodes.Ret);
		}

		public static ILGenerator DefinePublicMethod(this TypeBuilder tb, string name, Type returnType, IEnumerable<Type>? paramTypes = null)
		{
			var factory = tb.DefineMethod(name, MethodAttributes.Public
				| MethodAttributes.HideBySig
				| MethodAttributes.Virtual,
				returnType, paramTypes?.ToArray() ?? Array.Empty<Type>());
			return factory.GetILGenerator();
		}
		public static ILGenerator DefinePublicMethod(this TypeBuilder tb, MethodInfo method)
		{
			var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
			var factory = tb.DefineMethod(method.Name, MethodAttributes.Public
				| MethodAttributes.HideBySig
				| MethodAttributes.Virtual,
				method.ReturnType, paramTypes);

			if (method.IsGenericMethod)
			{
				// Define generic arguments and constraints
				var genParams = method.GetGenericArguments().Cast<TypeInfo>().ToArray();
				var methodGenPars = factory.DefineGenericParameters(genParams.Select(a => a.Name).ToArray());
				for (var i = 0; i < methodGenPars.Length; ++i)
				{
					// HACK: Reflection is reporting constraint interface hierarchy, which breaks compilation
					// Strip out parent interfaces in the hope the constraint doesn't specify them
					var interfaces = genParams[i].ImplementedInterfaces;
					interfaces = interfaces.Where(i => !interfaces.Any(ii => ii != i && i.IsAssignableFrom(ii))).ToArray();

					methodGenPars[i].SetBaseTypeConstraint(genParams[i].BaseType.RebuildGenericType(methodGenPars));
					methodGenPars[i].SetGenericParameterAttributes(genParams[i].GenericParameterAttributes);
					methodGenPars[i].SetInterfaceConstraints(interfaces.Select(i => i.RebuildGenericType(methodGenPars)).ToArray());
				}

				// Resolve T in parameter(s)
				paramTypes = paramTypes.Select(t => t.RebuildGenericType(methodGenPars)).ToArray();
				factory.SetParameters(paramTypes);

				// Resolve T in return
				factory.SetReturnType(factory.ReturnType.RebuildGenericType(methodGenPars));
			}

			return factory.GetILGenerator();
		}

		public static void EmitTypeShim(this ILGenerator impl, Type fromType, Type resultType)
		{
			if (fromType == typeof(void) || resultType == typeof(void)
				|| fromType.IsEquivalentGenericMethodType(resultType))
			{
				return;
			}

			if (fromType.IsValueType)
			{
				impl.Emit(OpCodes.Box, fromType);
			}
			var valType = resultType.IsArrayType() ? typeof(object[]) : typeof(object);
			var shimType = resultType.ResolveType();
			var shimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Shim), new[] { shimType }, new[] { valType });
			impl.Emit(OpCodes.Call, shimMethod);
		}

		public static void EmitTypeUnshim(this ILGenerator impl, Type shimType, Type realType)
		{
			if (shimType == realType || shimType == typeof(void) || realType == typeof(void))
			{
				return;
			}

			var valType = realType.IsArrayType() ? typeof(object[]) : typeof(object);
			var resultType = realType.ResolveType();
			var unshimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Unshim), new[] { resultType }, new[] { valType });
			impl.Emit(OpCodes.Call, unshimMethod);
		}

		public static void WrapField(this TypeBuilder tb, FieldInfo? instField, ShimBinding binding, FieldInfo fieldInfo)
		{
			var args = binding.InterfaceMethod.GetParameters();
			if (args.Length > 0 && (fieldInfo.Attributes & FieldAttributes.InitOnly) > 0)
			{
				// Set of readonly will be an exception
				tb.MethodThrowException<InvalidOperationException>(binding.InterfaceMethod);
				return;
			}

			var impl = tb.DefinePublicMethod(binding.InterfaceMethod);

			resolveIfInstance(fieldInfo.IsStatic, impl, instField);

			if (args.Length == 0)
			{
				// Get
				impl.Emit(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
				impl.EmitTypeShim(fieldInfo.FieldType, binding.InterfaceMethod.ReturnType);
			}
			else
			{
				// Set
				impl.Emit(OpCodes.Ldarg, 1);
				impl.EmitTypeUnshim(args[0].ParameterType, fieldInfo.FieldType);
				impl.Emit(fieldInfo.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
			}
			impl.Emit(OpCodes.Ret);
		}

		public static void WrapConstructor(this TypeBuilder tb, ShimBinding binding, ConstructorInfo constrInfo)
		{
			var impl = tb.DefinePublicMethod(binding.InterfaceMethod);

			resolveParameters(impl, constrInfo, binding.InterfaceMethod);
			impl.Emit(OpCodes.Newobj, constrInfo);
			var shimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Shim), new[] { binding.InterfaceMethod.ReturnType }, new[] { typeof(object) });
			impl.Emit(OpCodes.Call, shimMethod);
			impl.Emit(OpCodes.Ret);
		}

		public static void WrapMethod(this TypeBuilder tb, FieldInfo? instField, ShimBinding binding, MethodInfo methodInfo)
		{
			var impl = tb.DefinePublicMethod(binding.InterfaceMethod);

			if (binding.ProxyImplementationMember != null)
			{
				proxyMethodCall(impl, tb, instField, binding);
			}
			else
			{
				implMethodCall(impl, instField, binding.InterfaceMethod, methodInfo);
			}
		}
		private static void implMethodCall(ILGenerator impl, FieldInfo? instField, MethodInfo interfaceMethod, MethodInfo methodInfo)
		{
			var callType = !resolveIfInstance(methodInfo.IsStatic, impl, instField)
				? OpCodes.Call // Static
				: OpCodes.Callvirt;

			// Call implementation method
			resolveParameters(impl, methodInfo, interfaceMethod);
			impl.Emit(callType, methodInfo);
			impl.EmitTypeShim(methodInfo.ReturnType, interfaceMethod.ReturnType);
			impl.Emit(OpCodes.Ret);
		}
		private static void proxyMethodCall(ILGenerator impl, TypeBuilder tb, FieldInfo? instField, ShimBinding binding)
		{
			var proxyImplementation = binding.ProxyImplementationMember as MethodInfo ?? throw new NullReferenceException();
			var baseImplementation = binding.ImplementedMember as MethodInfo;

			if (baseImplementation == null)
			{
				// Call proxy method
				resolveParameters(impl, proxyImplementation, binding.InterfaceMethod, true);
				impl.Emit(OpCodes.Call, proxyImplementation);
				impl.EmitTypeShim(proxyImplementation.ReturnType, binding.InterfaceMethod.ReturnType);
				impl.Emit(OpCodes.Ret);
				return;
			}

			// Override implementation, with context
			var proxyField = tb.DefineField($"_inProxy_{binding.InterfaceMethod.Name}_{binding.InterfaceMethod.GetHashCode()}", typeof(bool), FieldAttributes.Private);

			// Check if in proxy context
			var jmpProxyCall = impl.DefineLabel();
			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(OpCodes.Ldfld, proxyField);
			impl.Emit(OpCodes.Brfalse, jmpProxyCall);
			implMethodCall(impl, instField, binding.InterfaceMethod, baseImplementation);
			impl.Emit(OpCodes.Ret);

			// Set proxy context
			impl.MarkLabel(jmpProxyCall);
			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(OpCodes.Ldc_I4_1);
			impl.Emit(OpCodes.Stfld, proxyField);

			// Call proxy method
			resolveParameters(impl, proxyImplementation, binding.InterfaceMethod, true);
			impl.Emit(OpCodes.Call, proxyImplementation);
			impl.EmitTypeShim(proxyImplementation.ReturnType, binding.InterfaceMethod.ReturnType);

			// Unset proxy context
			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(OpCodes.Ldc_I4_0);
			impl.Emit(OpCodes.Stfld, proxyField);
			impl.Emit(OpCodes.Ret);
		}

		public static void MethodThrowException<T>(this TypeBuilder tb, MethodInfo methodInfo)
			where T : Exception
		{
			var impl = tb.DefinePublicMethod(methodInfo);
			impl.Emit(OpCodes.Ldarg_0); // this

			var notImplementedConstr = typeof(T).GetConstructor(new Type[0]);
			impl.Emit(OpCodes.Newobj, notImplementedConstr);
			impl.Emit(OpCodes.Throw);
		}
	}
}
