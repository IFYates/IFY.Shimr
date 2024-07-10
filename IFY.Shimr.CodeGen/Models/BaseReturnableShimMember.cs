using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal abstract class BaseReturnableShimMember<T> : IShimMember
    where T : ISymbol
{
    public abstract string Name { get; }
    public abstract ITypeSymbol ReturnType { get; }
    public abstract string ReturnTypeName { get; }

    public virtual void GenerateCode(StringBuilder code, INamedTypeSymbol underlyingType)
    {
        var underlyingMember = GetUnderlyingMember(underlyingType);
        GenerateCode(code, underlyingType, underlyingMember);
    }
    public abstract void GenerateCode(StringBuilder code, INamedTypeSymbol underlyingType, T? underlyingMember);

    public string? GetShimCode(ITypeSymbol underlyingReturnType)
    {
        var shimReturnTypeName = ReturnType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        var underlyingReturnTypeName = underlyingReturnType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        var doShim = shimReturnTypeName != underlyingReturnTypeName;
        // TODO: check if shim registered

        return !doShim ? string.Empty : $".Shim<{ReturnTypeName}>()";
    }
    public string? GetUnshimCode(ITypeSymbol underlyingReturnType)
    {
        var shimReturnTypeName = ReturnType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        var underlyingReturnTypeName = underlyingReturnType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        var doShim = shimReturnTypeName != underlyingReturnTypeName;
        // TODO: check if shim registered

        return !doShim ? string.Empty : $".Unshim<{underlyingReturnTypeName}>()";
    }

    public virtual T? GetUnderlyingMember(INamedTypeSymbol underlyingType)
        => UnderlyingMemberMatch(underlyingType.GetMembers(Name).OfType<T>()).FirstOrDefault();
    protected virtual IEnumerable<T> UnderlyingMemberMatch(IEnumerable<T> underlyingMembers)
        => underlyingMembers;
}