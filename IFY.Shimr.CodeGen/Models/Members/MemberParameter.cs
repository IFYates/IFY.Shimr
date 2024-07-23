using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal class MemberParameter
{
    // TODO: out, ref, default
    // TODO: nullability?
    // TODO: attributes?

    public string Name { get; }
    public ITypeSymbol Type { get; }
    public ITypeSymbol? UnderlyingType { get; }
    public ITypeSymbol? UnderlyingElementType { get; }

    public MemberParameter(IParameterSymbol symbol)
    {
        Name = symbol.Name;
        Type = symbol.Type;
        UnderlyingType = symbol.GetArgumentShimType(true);
        if (UnderlyingType?.IsEnumerable(out var elementType) == true)
        {
            UnderlyingElementType = elementType;
        }
    }

    public string GetTargetArgumentCode()
    {
        return UnderlyingType == null
            ? Name
            : UnderlyingElementType != null
            ? $"{Name}.Unshim<{UnderlyingElementType.ToDisplayString()}>()"
            : $"({UnderlyingType.ToDisplayString()})((IShim){Name}).Unshim()";
    }

    public override string ToString()
        => $"{Type.ToDisplayString()} {Name}";

    public void RegisterOverride(ShimResolver shimResolver)
    {
        // Argument overrides
        if (UnderlyingType != null)
        {
            shimResolver.GetOrCreateShim(Type)
                .AddTarget(UnderlyingType);
        }
    }
}
