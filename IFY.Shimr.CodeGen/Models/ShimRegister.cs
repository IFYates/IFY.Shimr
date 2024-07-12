using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Pool of registered shims.
/// </summary>
internal class ShimRegister
{
    private readonly Dictionary<string, BaseShimType> _pool = [];
    public IEnumerable<BaseShimType> Types => _pool.Values;

    public ShimClassType GetOrCreate(ITypeSymbol interfaceType)
    {
        var key = interfaceType.ToDisplayString();
        if (!_pool.TryGetValue(key, out var shim))
        {
            _pool.Add(key, null!);
            shim = new ShimClassType(interfaceType);
            _pool[key] = shim;
        }
        return (ShimClassType)shim;
    }
    public ShimFactoryType GetOrCreateFactory(ITypeSymbol interfaceType)
    {
        var key = interfaceType.ToDisplayString();
        if (!_pool.TryGetValue(key, out var shim))
        {
            _pool.Add(key, null!);
            shim = new ShimFactoryType(interfaceType);
            _pool[key] = shim;
        }
        return (ShimFactoryType)shim;
    }

    /// <summary>
    /// Ensure that all implicit shims in registered shims are resolved.
    /// </summary>
    /// <returns>All current shims.</returns>
    public IEnumerable<IShimTarget> ResolveAllShims()
    {
        var shims = Types.SelectMany(s => s.Shims ?? []).ToList();
        var newShims = shims.ToList();
        while (newShims.Count > 0)
        {
            var loop = newShims.Distinct().ToArray();
            newShims.Clear();
            foreach (var shim in loop)
            {
                shim.ResolveImplicitShims(this, newShims);
            }
            newShims.RemoveAll(shims.Contains);
            shims.AddRange(newShims.Distinct());
        }
        return shims;
    }
}
