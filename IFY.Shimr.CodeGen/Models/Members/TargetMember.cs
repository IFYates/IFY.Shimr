using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

/// <summary>
/// A modelled member of the target type.
/// </summary>
internal abstract class TargetMember : IMember
{
    protected sealed class TargetFieldMember(ShimTarget target, IFieldSymbol symbol)
        : TargetMember(target, symbol, MemberType.Field)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.Type;
    }

    protected sealed class TargetPropertyMember(ShimTarget target, IPropertySymbol symbol)
        : TargetMember(target, symbol, MemberType.Property)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.Type;
    }

    protected class TargetMethodMember(ShimTarget target, IMethodSymbol symbol, MemberType type = MemberType.Method)
        : TargetMember(target, symbol, type), IParameterisedMember
    {
        // TODO
        public MemberParameter[] Parameters { get; }
            = symbol.Parameters.Select(p => new MemberParameter(p)).ToArray();

        public override ITypeSymbol? ReturnType { get; } = symbol.ReturnType;
    }

    protected sealed class TargetConstructorMember(ShimTarget target, IMethodSymbol symbol)
        : TargetMethodMember(target, symbol, MemberType.Constructor)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.ContainingType;
    }

    public static TargetMember? Parse(ISymbol symbol, ShimTarget target)
    {
        return symbol switch
        {
            IFieldSymbol field => new TargetFieldMember(target, field),
            IPropertySymbol property => new TargetPropertyMember(target, property),
            IMethodSymbol { MethodKind: MethodKind.Constructor } method => new TargetConstructorMember(target, method),
            IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation } method => new TargetMethodMember(target, method),
            IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.Destructor or MethodKind.UserDefinedOperator } => null,
            IMethodSymbol method => throw new NotSupportedException($"Unhandled method kind: {method.MethodKind}"),
            _ => throw new NotSupportedException($"Unhandled symbol type: {symbol.GetType().FullName}"),
        };
    }

    public ShimTarget Target { get; }
    public ISymbol Symbol { get; }
    public INamedTypeSymbol ContainingType { get; }
    public string Name { get; }
    public string FullName { get; }
    public MemberType Type { get; }
    public virtual ITypeSymbol? ReturnType { get; }

    private TargetMember(ShimTarget target, ISymbol symbol, MemberType type)
    {
        Target = target;
        Symbol = symbol;
        ContainingType = symbol.ContainingType;
        Name = symbol.Name;
        FullName = symbol.ToDisplayString();
        Type = type;
    }

    /// <summary>
    /// Returns true if the <see cref="ShimMember.ReturnType"/> of <paramref name="shimMember"/> is shimmable from the <see cref="ReturnType"/> of this target member.
    /// </summary>
    public bool IsShimmableReturnType(ShimMember shimMember)
        => IsShimmableReturnType(shimMember, out _, out _);
    public bool IsShimmableReturnType(ShimMember shimMember, out ITypeSymbol? targetElement, out ITypeSymbol? shimElement)
    {
        targetElement = null;
        shimElement = null;

        if (ReturnType == null || shimMember.ReturnType == null)
        {
            return ReturnType == null && shimMember.ReturnType == null;
        }
        if (ReturnType.IsMatch(shimMember.ReturnType))
        {
            return true;
        }
        if (ReturnType.IsEnumerable(out targetElement)
            && shimMember.ReturnType.IsEnumerable(out shimElement))
        {
            if (targetElement!.IsMatch(shimElement))
            {
                targetElement = null;
                shimElement = null;
                return true;
            }
            
            return shimElement!.TypeKind == TypeKind.Interface;
        }

        return false;
    }
}
