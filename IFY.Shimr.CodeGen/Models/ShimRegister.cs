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
}
