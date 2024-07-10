using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Models an interface used to shim another type.
/// </summary>
internal class ShimterfaceModel
{
    #region Object pool
    private static readonly Dictionary<string, ShimterfaceModel> _pool = [];
    public static int ShimterfaceCount => _pool.Count;

    public static Random R = new();
    public static ShimterfaceModel GetOrCreate(INamedTypeSymbol interfaceType)
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

    #endregion

    public INamedTypeSymbol InterfaceType { get; }
    public string InterfaceFullName { get; }
    public List<ShimModel> Shims { get; } = [];

    private IShimMember[]? _members = null;

    private ShimterfaceModel(INamedTypeSymbol interfaceType)
    {
        InterfaceType = interfaceType;
        InterfaceFullName = InterfaceType.ToDisplayString();
    }

    public ShimModel AddShim(INamedTypeSymbol underlyingType)
    {
        var shim = new ShimModel(this, underlyingType);
        if (!Shims.Any(s => s.Key == shim.Key))
        {
            Shims.Add(shim);
        }
        return shim;
    }

    // fields
    // X properties
    // - return shim
    // X methods
    // - argument shim
    // - return shim
    // static methods
    // constructors
    // events
    // renaming
    // implemented interface members

    public IShimMember[] ResolveShimMembers()
    {
        if (_members is null)
        {
            var members = new List<IShimMember>();
            foreach (var member in InterfaceType.GetMembers())
            {
                switch (member)
                {
                    case IMethodSymbol ms:
                        if (ms.MethodKind is not MethodKind.PropertyGet and not MethodKind.PropertySet)
                        {
                            members.Add(new ShimMethod(ms));
                        }
                        break;
                    case IPropertySymbol ps:
                        members.Add(new ShimProperty(ps));
                        break;
                    default:
                        ShimrSourceGenerator.WriteOutput($"// Unhandled member type: {member.GetType().FullName}");
                        break;
                }
            }
            _members = members.ToArray();
        }
        return _members;
    }
}
