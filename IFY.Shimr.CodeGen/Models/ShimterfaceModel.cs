using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Models an interface used to shim another type.
/// </summary>
internal class ShimterfaceModel
{
    public ITypeSymbol InterfaceType { get; }
    public string InterfaceFullName { get; }
    public List<ShimModel> Shims { get; } = [];

    private IShimMember[]? _members = null;

    public ShimterfaceModel(ITypeSymbol interfaceType)
    {
        InterfaceType = interfaceType;
        InterfaceFullName = InterfaceType.ToDisplayString();
    }

    public ShimModel AddShim(ITypeSymbol underlyingType)
    {
        var shim = new ShimModel(this, underlyingType);
        if (!Shims.Any(s => s.Key == shim.Key))
        {
            Shims.Add(shim);
        }
        return Shims.Single(s => s.Key == shim.Key);
    }

    public ShimFactoryModel AddShimFactory(ITypeSymbol underlyingType)
    {
        var shim = new ShimFactoryModel(this, underlyingType);
        if (!Shims.Any(s => s.Key == shim.Key))
        {
            Shims.Add(shim);
        }
        return (ShimFactoryModel)Shims.Single(s => s.Key == shim.Key);
    }

    // fields
    // X properties
    // X- return shim
    // X methods
    // - argument shim
    // X- return shim
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
                        Diag.WriteOutput($"// Unhandled member type: {member.GetType().FullName}");
                        break;
                }
            }
            _members = members.ToArray();
        }
        return _members;
    }
}
