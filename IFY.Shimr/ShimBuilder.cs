using IFY.Shimr.Extensions;
using IFY.Shimr.Internal;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace IFY.Shimr;

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

        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("IFY.Shimr.dynamic"), AssemblyBuilderAccess.Run);
        _mod = asm.DefineDynamicModule("IFY.Shimr.dynamic");
        _dynamicTypeCache.Clear();
        _ignoreMissingMembers.Clear();
    }

    private static readonly List<Type> _ignoreMissingMembers = [];
    private static ModuleBuilder _mod;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    static ShimBuilder()
    {
        ResetState();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Don't compile the type every time
    /// </summary>
    private static readonly Dictionary<string, Type> _dynamicTypeCache = [];

    #region Internal

    private static Type getShimType(Type interfaceType, Type implType)
    {
        var className = $"{implType.Name}_{implType.GetHashCode()}_{interfaceType.Name}_{interfaceType.GetHashCode()}";
        if (!_dynamicTypeCache.ContainsKey(className))
        {
            lock (interfaceType)
            {
                if (!_dynamicTypeCache.ContainsKey(className))
                {
                    var tb = _mod.DefineType($"Shim_{className}", TypeAttributes.Public
                        | TypeAttributes.AutoClass
                        | TypeAttributes.BeforeFieldInit, null, [typeof(IShim), interfaceType]);

                    var instField = tb.DefineField("_inst", implType, FieldAttributes.Private);

                    tb.AddConstructor(instField);
                    tb.AddUnshimMethod(instField);

                    // Proxy all methods (including events, properties, and indexers)
                    var methods = interfaceType.GetMethods()
                        .Union(interfaceType.GetInterfaces().SelectMany(static i => i.GetMethods()))
                        .Where(static m => m.IsAbstract).ToArray(); // Ignore default interface methods
                    foreach (var interfaceMethod in methods)
                    {
                        // Don't try to implement IShim
                        if (interfaceMethod.DeclaringType == typeof(IShim))
                        {
                            continue;
                        }

                        // Must not implement unsupported attributes
                        var attr = interfaceMethod.GetAttribute<StaticShimAttribute>();
                        if (attr != null)
                        {
                            throw new InvalidCastException($"Instance shim cannot implement static member: {interfaceType.FullName} {interfaceMethod.Name}");
                        }

                        shimMember(tb, instField, implType, interfaceMethod, false);
                    }

                    _dynamicTypeCache.Add(className, tb.CreateType());
                }
            }
        }
        return _dynamicTypeCache[className];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Method is internal for testing")]
    internal static Type getFactoryType(Type interfaceType)
    {
        var className = $"{interfaceType.Name}_{interfaceType.GetHashCode()}";
        if (!_dynamicTypeCache.ContainsKey(className))
        {
            lock (interfaceType)
            {
                if (!_dynamicTypeCache.ContainsKey(className))
                {
                    // Find static source on interface
                    var intAttr = interfaceType.GetCustomAttributes(typeof(StaticShimAttribute), false).OfType<StaticShimAttribute>().FirstOrDefault();
                    if (intAttr?.IsConstructor == true) // Cannot define full interface as constructor
                    {
                        throw new NotSupportedException($"Factory interface cannot be marked as constructor shim: {interfaceType.FullName}");
                    }

                    var tb = _mod.DefineType(className, TypeAttributes.Public
                        | TypeAttributes.AutoClass
                        | TypeAttributes.BeforeFieldInit, null, [interfaceType]);

                    // Proxy all methods (including events, properties, and indexers)
                    var methods = interfaceType.GetMethods()
                        .Where(static m => m.IsAbstract).ToArray(); // Ignore default interface methods
                    foreach (var interfaceMethod in methods)
                    {
                        // Must define static source, if not at interface
                        var attr = interfaceMethod.GetAttribute<StaticShimAttribute>()
                            ?? intAttr
                            ?? throw new InvalidCastException($"Factory shim cannot implement non-static member: {interfaceType.FullName} {interfaceMethod.Name}");
                        var implType = attr.TargetType ?? intAttr!.TargetType ?? typeof(void);
                        shimMember(tb, null, implType, interfaceMethod, attr.IsConstructor);
                    }

                    _dynamicTypeCache.Add(className, tb.CreateType());
                }
            }
        }
        return _dynamicTypeCache[className];
    }

    private static void shimMember(TypeBuilder tb, FieldBuilder? instField, Type implType, MethodInfo interfaceMethod, bool isConstructor)
    {
        // Match real member
        var binding = new ShimBinding(interfaceMethod);
        if (!binding.Resolve(implType, isConstructor))
        {
            if (_ignoreMissingMembers.Contains(interfaceMethod.DeclaringType))
            {
                tb.MethodThrowException<NotImplementedException>(interfaceMethod);
                return;
            }

            throw new MissingMemberException($"Cannot shim {implType.FullName} as {interfaceMethod.DeclaringType.FullName}; missing method: {interfaceMethod}");
        }

        // Generate proxy
        var member = binding.ImplementedMember ?? binding.ProxyImplementationMember!;
        switch (member)
        {
            case ConstructorInfo constrInfo:
                tb.WrapConstructor(binding, constrInfo);
                return;
            case FieldInfo fieldInfo:
                tb.WrapField(instField, binding, fieldInfo);
                return;
            default:
                tb.WrapMethod(instField, binding, (MethodInfo)member);
                return;
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
        return (TInterface)Create(typeof(TInterface));
    }

    /// <summary>
    /// Create a factory proxy.
    /// Type <paramref name="interfaceType"/> must only implement methods decorated with <see cref="StaticShimAttribute"/>.
    /// </summary>
    public static object Create(Type interfaceType)
    {
        var factoryType = getFactoryType(interfaceType);
        return Activator.CreateInstance(factoryType);
    }

    #endregion Create

    #region Shim

    // NOTE: Used internally

    /// <summary>
    /// Converts the result of a completed task to a specified type using a shim operation.
    /// </summary>
    /// <remarks>This method is intended for advanced scenarios where dynamic type conversion of task results
    /// is required. The conversion is performed after the input task completes. If the conversion fails, the returned
    /// task will be faulted with the corresponding exception.</remarks>
    /// <typeparam name="TSource">The type of the result produced by the input task.</typeparam>
    /// <typeparam name="TResult">The type to which the result of the input task will be converted.</typeparam>
    /// <param name="inst">The task whose result will be converted. Must be completed successfully before conversion.</param>
    /// <returns>A task that represents the asynchronous conversion operation. The result of the returned task is the converted
    /// value of type TResult.</returns>
    public static async Task<TResult> Shim<TSource, TResult>(Task<TSource> inst)
    {
        return (TResult)Shim(typeof(TResult), await inst)!;
    }

    /// <summary>
    /// Converts the result of a completed task to a specified type using a shim operation.
    /// </summary>
    /// <remarks>This method is intended for advanced scenarios where dynamic type conversion of task results
    /// is required. The conversion is performed after the input task completes. If the conversion fails, the returned
    /// task will be faulted with the corresponding exception.</remarks>
    /// <typeparam name="TSource">The type of the result produced by the input task.</typeparam>
    /// <typeparam name="TResult">The type to which the result of the input task will be converted.</typeparam>
    /// <param name="inst">The task whose result will be converted. Must be completed successfully before conversion.</param>
    /// <returns>A task that represents the asynchronous conversion operation. The result of the returned task is the converted
    /// value of type TResult.</returns>
    public static async ValueTask<TResult> Shim<TSource, TResult>(ValueTask<TSource> inst)
    {
        return (TResult)Shim(typeof(TResult), await inst)!;
    }

    /// <summary>
    /// Use a shim to make the given object look like the required type.
    /// Result will also implement <see cref="IShim"/>.
    /// </summary>
    public static TInterface? Shim<TInterface>(object? inst)
        where TInterface : class
    {
        return (TInterface?)Shim(typeof(TInterface), inst);
    }

    /// <summary>
    /// Use a shim to make the given objects look like the required type.
    /// Results will also implement <see cref="IShim"/>.
    /// </summary>
    public static TInterface?[]? Shim<TInterface>(IEnumerable<object>? inst)
        where TInterface : class
    {
        return inst?.Select(static i => (TInterface?)Shim(typeof(TInterface), i)).ToArray();
    }

    /// <summary>
    /// Use a shim to make the given object look like the required type.
    /// Result will also implement <see cref="IShim"/>.
    /// </summary>
    public static object? Shim(Type interfaceType, object? inst)
    {
        if (inst == null)
        {
            return null;
        }

        // Run-time test that type is an interface
        if (!interfaceType.IsInterface)
        {
            throw new NotSupportedException($"Generic argument must be a direct interface: {interfaceType.FullName}");
        }

        if (interfaceType.IsAssignableFrom(inst.GetType()))
        {
            return inst;
        }

        var shimType = getShimType(interfaceType, inst.GetType());
        var shim = Activator.CreateInstance(shimType, [inst]);
        return shim;
    }

    #endregion Shim

    #region Unshim

    // NOTE: Used internally

    /// <summary>
    /// Recast shim to original type.
    /// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
    /// </summary>
    public static T Unshim<T>(object shim)
    {
        return (T)(shim is IShim s ? s.Unshim() : shim);
    }

    /// <summary>
    /// Recast shims to original type.
    /// No type-safety checks. Must already be <typeparamref name="T"/> or be <see cref="IShim"/> of <typeparamref name="T"/>.
    /// </summary>
    public static T[] Unshim<T>(IEnumerable<object> shims)
        => shims.Unshim<T>().ToArray();

    #endregion Unshim
}
