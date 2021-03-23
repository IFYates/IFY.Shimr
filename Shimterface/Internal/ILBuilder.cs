using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shimterface.Internal
{
	internal static class ILBuilder
	{
		private static bool resolveIfInstance(ILGenerator impl, FieldBuilder? instField)
		{
			if (instField == null)
			{
				return false;
			}

			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(instField.FieldType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, instField);
			return true;
		}
		private static void resolveParameters(ILGenerator impl, MethodBase methodInfo, MethodInfo interfaceMethod)
		{
			// Pass each parameter from the method call to the implementation
			var pars1 = methodInfo.GetParameters();
			var pars2 = interfaceMethod.GetParameters();
			for (var i = 0; i < pars1.Length; ++i)
			{
				impl.Emit(OpCodes.Ldarg, i + 1);
				impl.EmitTypeUnshim(pars2[i].ParameterType, pars1[i].ParameterType);
			}
		}

		public static void AddConstructor(this TypeBuilder tb, FieldBuilder instField)
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

		public static void AddUnshimMethod(this TypeBuilder tb, FieldBuilder instField)
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

		public static bool EmitTypeShim(this ILGenerator impl, Type fromType, Type resultType)
		{
			if (fromType == typeof(void) || resultType == typeof(void)
				|| fromType.IsEquivalentGenericMethodType(resultType))
			{
				return false;
			}

			if (fromType.IsValueType)
			{
				impl.Emit(OpCodes.Box, fromType);
			}
			var valType = resultType.IsArrayType() ? typeof(object[]) : typeof(object);
			var shimType = resultType.ResolveType();
			var shimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Shim), new[] { shimType }, new[] { valType });
			impl.Emit(OpCodes.Call, shimMethod);
			return true;
		}

		public static bool EmitTypeUnshim(this ILGenerator impl, Type shimType, Type realType)
		{
			if (shimType == realType || shimType == typeof(void) || realType == typeof(void))
			{
				return false;
			}

			var valType = realType.IsArrayType() ? typeof(object[]) : typeof(object);
			var resultType = realType.ResolveType();
			var unshimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Unshim), new[] { resultType }, new[] { valType });
			impl.Emit(OpCodes.Call, unshimMethod);
			return true;
		}

		public static void FieldWrap(this TypeBuilder tb, FieldBuilder? instField, MethodInfo interfaceMethod, FieldInfo fieldInfo)
		{
			var args = interfaceMethod.GetParameters();
			if (args.Length > 0 && (fieldInfo.Attributes & FieldAttributes.InitOnly) > 0)
			{
				// Set of readonly will be an exception
				tb.MethodThrowException<InvalidOperationException>(interfaceMethod);
				return;
			}

			var impl = tb.DefinePublicMethod(interfaceMethod);

			resolveIfInstance(impl, instField);

			if (args.Length == 0)
			{
				// Get
				impl.Emit(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
				impl.EmitTypeShim(fieldInfo.FieldType, interfaceMethod.ReturnType);
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

		public static void MethodCall(this TypeBuilder tb, MethodInfo interfaceMethod, ConstructorInfo constrInfo)
		{
			var impl = tb.DefinePublicMethod(interfaceMethod);

			resolveParameters(impl, constrInfo, interfaceMethod);
			impl.Emit(OpCodes.Newobj, constrInfo);
			var shimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Shim), new[] { interfaceMethod.ReturnType }, new[] { typeof(object) });
			impl.Emit(OpCodes.Call, shimMethod);
			impl.Emit(OpCodes.Ret);
		}

		public static void MethodCall(this TypeBuilder tb, FieldBuilder? instField, MethodInfo interfaceMethod, MethodInfo methodInfo)
		{
			var impl = tb.DefinePublicMethod(interfaceMethod);

			var callType = !resolveIfInstance(impl, instField)
				? OpCodes.Call // Static
				: OpCodes.Callvirt;

			resolveParameters(impl, methodInfo, interfaceMethod);
			impl.Emit(callType, methodInfo);
			impl.EmitTypeShim(methodInfo.ReturnType, interfaceMethod.ReturnType);
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
