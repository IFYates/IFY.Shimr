using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shimterface.Standard.Internal
{
	internal static class ILBuilder
	{
		private static bool resolveIfInstance(ILGenerator impl, FieldBuilder instField)
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
				| MethodAttributes.RTSpecialName, CallingConventions.Standard, new[] { instField.FieldType });
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

		public static MethodBuilder DefinePublicMethod(this TypeBuilder tb, string name, Type returnType, IEnumerable<Type> typeParams = null)
		{
			return tb.DefineMethod(name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, returnType, typeParams?.ToArray() ?? Array.Empty<Type>());
		}
		
		public static bool EmitTypeShim(this ILGenerator impl, Type fromType, Type resultType)
		{
			if (fromType == resultType || fromType == typeof(void) || resultType == typeof(void))
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

		public static void FieldWrap(this TypeBuilder tb, FieldBuilder instField, MethodInfo interfaceMethod, FieldInfo fieldInfo)
		{
			var args = interfaceMethod.GetParameters();
			if (args.Length > 0 && (fieldInfo.Attributes & FieldAttributes.InitOnly) > 0)
			{
				// Set of readonly will be an exception
				tb.MethodThrowException<InvalidOperationException>(interfaceMethod);
				return;
			}

			var method = tb.DefinePublicMethod(interfaceMethod.Name, interfaceMethod.ReturnType, interfaceMethod.GetParameters().Select(p => p.ParameterType));
			var impl = method.GetILGenerator();

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
			var method = tb.DefinePublicMethod(interfaceMethod.Name, interfaceMethod.ReturnType, interfaceMethod.GetParameters().Select(p => p.ParameterType));
			var impl = method.GetILGenerator();

			resolveParameters(impl, constrInfo, interfaceMethod);
			impl.Emit(OpCodes.Newobj, constrInfo);
			var shimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Shim), new[] { interfaceMethod.ReturnType }, new[] { typeof(object) });
			impl.Emit(OpCodes.Call, shimMethod);
			impl.Emit(OpCodes.Ret);
		}

		public static void MethodCall(this TypeBuilder tb, FieldBuilder instField, MethodInfo interfaceMethod, MethodInfo methodInfo)
		{
			var method = tb.DefinePublicMethod(interfaceMethod.Name, interfaceMethod.ReturnType, interfaceMethod.GetParameters().Select(p => p.ParameterType));
			var impl = method.GetILGenerator();

			var callType = OpCodes.Call; // Static
			if (resolveIfInstance(impl, instField))
			{
				callType = OpCodes.Callvirt;
			}

			resolveParameters(impl, methodInfo, interfaceMethod);
			impl.Emit(callType, methodInfo);
			impl.EmitTypeShim(methodInfo.ReturnType, interfaceMethod.ReturnType);
			impl.Emit(OpCodes.Ret);
		}

		public static void MethodThrowException<T>(this TypeBuilder tb, MethodInfo methodInfo)
			where T : Exception
		{
			var method = tb.DefinePublicMethod(methodInfo.Name, methodInfo.ReturnType, methodInfo.GetParameters().Select(p => p.ParameterType));
			var impl = method.GetILGenerator();
			impl.Emit(OpCodes.Ldarg_0); // this

			var notImplementedConstr = typeof(T).GetConstructor(new Type[0]);
			impl.Emit(OpCodes.Newobj, notImplementedConstr);
			impl.Emit(OpCodes.Throw);
		}

		public static void MethodUnshim(this TypeBuilder tb, FieldBuilder instField)
		{
			// object Unshim()
			var unshimMethod = tb.DefinePublicMethod("Unshim", typeof(object));
			var impl = unshimMethod.GetILGenerator();
			impl.Emit(OpCodes.Ldarg_0); // this
			impl.Emit(OpCodes.Ldfld, instField);
			if (instField.FieldType.IsValueType)
			{
				impl.Emit(OpCodes.Box, instField.FieldType);
			}
			impl.Emit(OpCodes.Ret);
		}
	}
}
