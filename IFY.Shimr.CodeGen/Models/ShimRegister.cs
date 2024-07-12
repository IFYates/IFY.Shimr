using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Pool of registered shims.
/// </summary>
internal class ShimRegister
{
    public static Random R = new();

    private readonly Dictionary<string, ShimterfaceModel> _pool = [];
    public IEnumerable<ShimterfaceModel> Interfaces => _pool.Values;

    public ShimterfaceModel GetOrCreate(ITypeSymbol interfaceType)
    {
        var key = interfaceType.ToDisplayString();
        if (!_pool.TryGetValue(key, out var shim))
        {
            _pool.Add(key, null!);
            shim = new ShimterfaceModel(interfaceType);
            _pool[key] = shim;
        }
        return shim;
    }

    /// <summary>
    /// Ensure that all implicit shims in registered shims are resolved.
    /// </summary>
    /// <returns>All current shims.</returns>
    public IEnumerable<ShimModel> ResolveAllShims()
    {
        var shims = Interfaces.SelectMany(s => s.Shims ?? []).ToList();
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
