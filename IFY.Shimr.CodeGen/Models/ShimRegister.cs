using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

// TODO: combine with ShimResolver
/// <summary>
/// Pool of registered shims.
/// </summary>
internal class ShimRegister
{
    private readonly Dictionary<string, IShimDefinition> _pool = [];
    public IEnumerable<IShimDefinition> Definitions => _pool.Values;

    // TODO: cannot mix static and instance types

    public IShimDefinition GetOrCreate(ITypeSymbol interfaceType, bool asFactory)
    {
        lock (_pool)
        {
            var key = interfaceType.ToDisplayString();
            if (!_pool.TryGetValue(key, out var shim))
            {
                shim = !asFactory
                    ? new InstanceShimDefinition(interfaceType)
                    : new ShimFactoryDefinition(interfaceType);
                _pool.Add(key, shim);
            }
            if (!asFactory && shim is not InstanceShimDefinition)
            {
                Diag.WriteOutput("// Got factory as instance shim: " + interfaceType.ToDisplayString());
            }
            return shim;
        }
    }
    public InstanceShimDefinition GetOrCreateShim(ITypeSymbol interfaceType)
        => (InstanceShimDefinition)GetOrCreate(interfaceType, false);
    public ShimFactoryDefinition GetOrCreateFactory(ITypeSymbol interfaceType)
        => (ShimFactoryDefinition)GetOrCreate(interfaceType, true);

    /// <summary>
    /// Ensure that all implicit shims in registered shims are resolved.
    /// </summary>
    /// <returns>All current shims.</returns>
    public IList<IBinding> ResolveAllShims(CodeErrorReporter errors, ShimRegister shimRegister)
    {
        var bindings = new List<IBinding>();
        var shimsDone = new List<IShimDefinition>();
        var newShims = _pool.Values.Except(shimsDone).ToArray();
        while (newShims.Any())
        {
            foreach (var shimType in newShims)
            {
                shimType.Resolve(bindings, errors, shimRegister);
            }
            shimsDone.AddRange(newShims);
            newShims = _pool.Values.Except(shimsDone).ToArray();
        }

        return bindings;
    }
}
