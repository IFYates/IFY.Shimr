﻿using System.Reflection;
using System.Reflection.Emit;

namespace IFY.Shimr.Internal;

internal static class ILBuilder
{
    private static bool resolveIfInstance(bool isStatic, ILGenerator impl, FieldInfo? instField)
    {
        if (isStatic)
        {
            return false;
        }

        impl.Emit(OpCodes.Ldarg_0); // this
        impl.Emit(instField!.FieldType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, instField);
        return true;
    }
    private static void resolveParameters(ILGenerator impl, MethodBase methodInfo, MethodInfo interfaceMethod)
    {
        var pars1 = methodInfo.GetParameters().ToArray();
        var pars2 = interfaceMethod.GetParameters();

        // Proxies take "this" as first arg
        var p1 = 0;
        if (pars1.Length == pars2.Length + 1 && pars1[0].ParameterType.IsAssignableFrom(interfaceMethod.DeclaringType))
        {
            impl.Emit(OpCodes.Ldarg_0); // this
            ++p1;
        }

        // Pass each parameter from the method call to the implementation
        for (byte p2 = 0; p1 < pars1.Length; ++p1, ++p2)
        {
            impl.Ldarg((byte)(p2 + 1));
            impl.EmitTypeUnshim(pars2[p2].ParameterType, pars1[p1].ParameterType);
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
        constr.DefineParameter(1, ParameterAttributes.None, string.Empty);
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
    public static MethodBuilder DefinePublicMethod(this TypeBuilder tb, MethodInfo method, out Type[] paramTypes)
    {
        paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var factory = tb.DefineMethod(method.Name, MethodAttributes.Public
            | MethodAttributes.HideBySig
            | MethodAttributes.Virtual,
            method.ReturnType, paramTypes);

        if (method.IsGenericMethodDefinition)
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

        return factory;
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

        var argType = typeof(object);
        var shimType = resultType;
        if (resultType.IsArrayType(out var resultElementType) && fromType.IsArrayType(out var fromElementType)
            && resultElementType != fromElementType)
        {
            argType = typeof(IEnumerable<object>); // Auto-shim collection
            shimType = resultElementType;
        }

        var shimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Shim), new[] { shimType }, new[] { argType });
        impl.Emit(OpCodes.Call, shimMethod);
    }

    public static void EmitTypeUnshim(this ILGenerator impl, Type shimType, Type realType)
    {
        if (shimType == realType || shimType == typeof(void) || realType == typeof(void))
        {
            return;
        }

        var valType = realType.IsArrayType(out _) ? typeof(IEnumerable<object>) : typeof(object);
        var resultType = realType.ResolveType();
        var unshimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Unshim), new[] { resultType }, new[] { valType });
        impl.Emit(OpCodes.Call, unshimMethod);
    }

    public static void WrapConstructor(this TypeBuilder tb, ShimBinding binding, ConstructorInfo constrInfo)
    {
        var factory = tb.DefinePublicMethod(binding.InterfaceMethod, out var argTypes);
        var impl = factory.GetILGenerator();
        var genericParams = factory.GetGenericArguments();

        if (constrInfo.DeclaringType.IsGenericTypeDefinition)
        {
            // Build args array
            var argsArr = impl.DeclareLocal(typeof(object[]));
            impl.Ldc_I4(argTypes.Length);
            impl.Emit(OpCodes.Newarr, typeof(object));
            for (var i = 0; i < argTypes.Length; ++i)
            {
                impl.Emit(OpCodes.Dup);
                impl.Ldc_I4(i);
                impl.Ldarg((byte)(i + 1));
                impl.Emit(OpCodes.Box, argTypes[i]);
                impl.Emit(OpCodes.Stelem_Ref);
            }
            impl.Stloc(argsArr.LocalIndex);

            // Build target type
            var resultType = constrInfo.DeclaringType.RebuildGenericType(genericParams);
            impl.Emit(OpCodes.Ldtoken, resultType);
            impl.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) }));
            impl.Ldloc(argsArr.LocalIndex);
            impl.Emit(OpCodes.Call, typeof(Activator).GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type), typeof(object[]) }));
        }
        else
        {
            resolveParameters(impl, constrInfo, binding.InterfaceMethod);
            impl.Emit(OpCodes.Newobj, constrInfo);
        }

        // Shim
        var shimMethod = typeof(ShimBuilder).BindStaticMethod(nameof(ShimBuilder.Shim), new[] { factory.ReturnType }, new[] { typeof(object) });
        impl.Emit(OpCodes.Call, shimMethod);

        impl.Emit(OpCodes.Ret);
    }

    public static void WrapField(this TypeBuilder tb, FieldInfo? instField, ShimBinding binding, FieldInfo fieldInfo)
    {
        var args = binding.InterfaceMethod.GetParameters();
        if (args.Length > 0 && (fieldInfo.Attributes & FieldAttributes.InitOnly) != 0)
        {
            // Set of readonly will be an exception
            tb.MethodThrowException<InvalidOperationException>(binding.InterfaceMethod);
            return;
        }

        var impl = tb.DefinePublicMethod(binding.InterfaceMethod, out _).GetILGenerator();

        if (binding.ProxyImplementationMember != null)
        {
            proxyMemberCall(impl, tb, instField, binding);
            return;
        }

        implFieldCall(impl, instField, binding, fieldInfo);
    }

    public static void WrapMethod(this TypeBuilder tb, FieldInfo? instField, ShimBinding binding, MethodInfo methodInfo)
    {
        var impl = tb.DefinePublicMethod(binding.InterfaceMethod, out _).GetILGenerator();

        if (binding.ProxyImplementationMember != null)
        {
            proxyMemberCall(impl, tb, instField, binding);
            return;
        }

        implMethodCall(impl, instField, binding.InterfaceMethod, methodInfo);
    }

    private static void implFieldCall(ILGenerator impl, FieldInfo? instField, ShimBinding binding, FieldInfo fieldInfo)
    {
        resolveIfInstance(fieldInfo.IsStatic, impl, instField);

        var args = binding.InterfaceMethod.GetParameters();
        if (args.Length == 0)
        {
            // Get
            impl.Emit(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
            impl.EmitTypeShim(fieldInfo.FieldType, binding.InterfaceMethod.ReturnType);
        }
        else
        {
            // Set
            impl.Emit(OpCodes.Ldarg_1);
            impl.EmitTypeUnshim(args[0].ParameterType, fieldInfo.FieldType);
            impl.Emit(fieldInfo.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
        }
        impl.Emit(OpCodes.Ret);
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
    private static void proxyMemberCall(ILGenerator impl, TypeBuilder tb, FieldInfo? instField, ShimBinding binding)
    {
        var proxyImplementation = binding.ProxyImplementationMember as MethodInfo ?? throw new NullReferenceException();

        if (binding.ImplementedMember == null)
        {
            // Call static proxy method
            resolveParameters(impl, proxyImplementation, binding.InterfaceMethod);
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
        if (binding.ImplementedMember is FieldInfo fi)
        {
            implFieldCall(impl, instField, binding, fi);
        }
        else
        {
            implMethodCall(impl, instField, binding.InterfaceMethod, (MethodInfo)binding.ImplementedMember);
        }

        // Set proxy context
        impl.MarkLabel(jmpProxyCall);
        impl.BeginExceptionBlock();
        impl.Emit(OpCodes.Ldarg_0); // this
        impl.Emit(OpCodes.Ldc_I4_1);
        impl.Emit(OpCodes.Stfld, proxyField);

        // Call proxy method
        resolveParameters(impl, proxyImplementation, binding.InterfaceMethod);
        impl.Emit(OpCodes.Call, proxyImplementation);
        impl.EmitTypeShim(proxyImplementation.ReturnType, binding.InterfaceMethod.ReturnType);

        // Unset proxy context
        impl.BeginFinallyBlock();
        impl.Emit(OpCodes.Ldarg_0); // this
        impl.Emit(OpCodes.Ldc_I4_0);
        impl.Emit(OpCodes.Stfld, proxyField);
        impl.EndExceptionBlock();

        impl.Emit(OpCodes.Ret);
    }

    public static void MethodThrowException<T>(this TypeBuilder tb, MethodInfo methodInfo)
        where T : Exception
    {
        var impl = tb.DefinePublicMethod(methodInfo, out _).GetILGenerator();
        impl.Emit(OpCodes.Ldarg_0); // this

        var notImplementedConstr = typeof(T).GetConstructor(new Type[0]);
        impl.Emit(OpCodes.Newobj, notImplementedConstr);
        impl.Emit(OpCodes.Throw);
    }
}
