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
    public IShimTarget[] ResolveAllShims()
    {
        var count = _pool.Values.Sum(s => s.Shims.Count());
        while (_resolveCount != count)
        {
            _resolveCount = count;
            foreach (var shimType in _pool.Values.ToArray())
            {
                shimType.ResolveImplicitShims(this);
            }
            count = _pool.Values.Sum(s => s.Shims.Count());
        }

        return _pool.Values.SelectMany(s => s.Shims).ToArray();
    }
    private int _resolveCount = 0;
}
