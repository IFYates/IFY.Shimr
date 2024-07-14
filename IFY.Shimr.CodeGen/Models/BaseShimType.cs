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

    private IShimMember[]? _members = null;

    private readonly Dictionary<string, IShimTarget> _targets = [];
    public IEnumerable<IShimTarget> Shims => _targets.Values;
    protected T AddTarget<T>(T target)
        where T : IShimTarget
    {
        lock (_targets)
        {
            if (!_targets.ContainsKey(target.Name))
            {
                _targets.Add(target.Name, target);
            }
            return (T)_targets[target.Name];
        }
    }
    public abstract void GenerateCode(StringBuilder code, CodeErrorReporter errors);

    // TODO
    // fields
    // X properties
    // X- return shim
    // X methods
    // X- argument shim
    // - argument array shim
    // - argument ienum shim
    // X- return shim
    // X- return array
    // X- return ienum
    // X static types
    // X static methods
    // constructors
    // events
    // renaming
    // implemented interface members

    public IShimMember[] ResolveShimMembers()
    {
        if (_members is null)
        {
            var members = new List<IShimMember>();
            foreach (var member in InterfaceType.GetAllMembers())
            {
                switch (member)
                {
                    case IMethodSymbol ms:
                        if (ms.MethodKind is not MethodKind.PropertyGet and not MethodKind.PropertySet
                            && ms.IsAbstract) // Ignore implemented interface members
                        {
                            members.Add(new ShimMemberMethod(this, ms));
                        }
                        break;
                    case IPropertySymbol ps:
                        if (!ps.ExplicitInterfaceImplementations.Any())
                        {
                            members.Add(new ShimMemberProperty(this, ps));
                        }
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

    public void ResolveImplicitShims(ShimRegister shimRegister)
    {
        foreach (var target in _targets.Values)
        {
            target.ResolveImplicitShims(shimRegister);
        }
    }
}
