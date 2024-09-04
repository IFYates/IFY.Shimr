using IFY.Shimr.SourceGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen.Models.Members;

/// <summary>
/// A modelled member of the target type.
/// </summary>
internal abstract class TargetMember : IMember
{
    protected sealed class TargetEventMember(ShimTarget target, IEventSymbol symbol)
        : TargetMember(target, symbol, MemberType.Event)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.Type;
    }

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
        public MemberParameter[] Parameters { get; } = symbol.Parameters.Select(p => new MemberParameter(p)).ToArray();

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
            IEventSymbol eventSymbol => new TargetEventMember(target, eventSymbol),
            IPropertySymbol property => new TargetPropertyMember(target, property),
            IMethodSymbol { MethodKind: MethodKind.Constructor } method => new TargetConstructorMember(target, method),
            IMethodSymbol { MethodKind: MethodKind.ExplicitInterfaceImplementation or MethodKind.Ordinary } method => new TargetMethodMember(target, method),
            IMethodSymbol { MethodKind: MethodKind.Conversion or MethodKind.Destructor or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.StaticConstructor or MethodKind.UserDefinedOperator } => null,
            IMethodSymbol method => throw new NotSupportedException($"Unhandled method kind: {method.MethodKind}"),
            _ => throw new NotSupportedException($"Unhandled symbol type: {symbol.GetType().FullName}"),
        };
    }

    public ShimTarget Target { get; }
    public ISymbol Symbol { get; }
    public MemberType Type { get; }
    public INamedTypeSymbol ContainingType => Symbol.ContainingType;
    public string Name => Symbol.Name;
    public bool IsStatic => Symbol.IsStatic;
    public virtual ITypeSymbol? ReturnType { get; }

    private TargetMember(ShimTarget target, ISymbol symbol, MemberType type)
    {
        Target = target;
        Symbol = symbol;
        Type = type;
    }

    /// <summary>
    /// Returns true if the <see cref="IMember.ReturnType"/> of <paramref name="otherMember"/> is shimmable from the <see cref="ReturnType"/> of this target member.
    /// </summary>
    public bool IsShimmableReturnType(IMember otherMember)
        => IsShimmableReturnType(otherMember, out _, out _);
    public bool IsShimmableReturnType(IMember otherMember, out ITypeSymbol? targetElement, out ITypeSymbol? otherElement)
    {
        targetElement = null;
        otherElement = null;

        if (ReturnType == null || otherMember.ReturnType == null)
        {
            return ReturnType == null && otherMember.ReturnType == null;
        }
        if (ReturnType.IsMatch(otherMember.ReturnType))
        {
            return true;
        }
        if (ReturnType.IsEnumerable(out targetElement)
            && otherMember.ReturnType.IsEnumerable(out otherElement))
        {
            if (targetElement!.IsMatch(otherElement))
            {
                targetElement = null;
                otherElement = null;
                return true;
            }

            return otherElement!.TypeKind == TypeKind.Interface;
        }

        return false;
    }
}
