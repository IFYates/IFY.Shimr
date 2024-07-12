using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Models an interface used to shim another type.
/// </summary>
internal abstract class BaseShimType(ITypeSymbol interfaceType)
{
    public virtual string Name { get; } = interfaceType.Name;
    public ITypeSymbol InterfaceType { get; } = interfaceType;
    public string InterfaceFullName { get; } = interfaceType.ToDisplayString();

    private readonly Dictionary<string, IShimTarget> _shims = [];
    public IEnumerable<IShimTarget> Shims => _shims.Values;
    protected T AddTarget<T>(T shim)
        where T : IShimTarget
    {
        lock (_shims)
        {
            if (!_shims.ContainsKey(shim.Name))
            {
                _shims.Add(shim.Name, shim);
            }
            return (T)_shims[shim.Name];
        }
    }

    private IShimMember[]? _members = null;

    // fields
    // X properties
    // X- return shim
    // X methods
    // - argument shim
    // X- return shim
    // X static types
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
                            members.Add(new ShimMemberMethod(ms));
                        }
                        break;
                    case IPropertySymbol ps:
                        members.Add(new ShimMemberProperty(ps));
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

    public abstract void GenerateCode(StringBuilder code, CodeErrorReporter errors);
}
