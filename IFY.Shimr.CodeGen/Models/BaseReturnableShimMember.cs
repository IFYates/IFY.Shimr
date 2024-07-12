using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal abstract class BaseReturnableShimMember<T> : IShimMember, IReturnableShimMember
    where T : ISymbol
{
    public abstract string Name { get; }
    public abstract ITypeSymbol ReturnType { get; }
    public abstract string ReturnTypeName { get; }

    public abstract ITypeSymbol GetUnderlyingMemberReturn(ITypeSymbol underlyingType);

    public void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType)
    {
        var underlyingMember = GetUnderlyingMember(underlyingType);
        GenerateCode(code, errors, underlyingType, underlyingMember);
    }
    public abstract void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType, T? underlyingMember);

    public string? GetShimCode(ITypeSymbol underlyingReturnType)
    {
        var doShim = !ReturnType.IsMatch(underlyingReturnType);
        // TODO: check if shim registered

        return !doShim ? string.Empty : $".Shim<{ReturnTypeName}>()";
    }
    public string? GetUnshimCode(ITypeSymbol underlyingReturnType)
    {
        var doShim = !ReturnType.IsMatch(underlyingReturnType);
        // TODO: check if shim registered

        return !doShim ? string.Empty : $".Unshim<{underlyingReturnType.ToDisplayString()}>()";
    }

    public T? GetUnderlyingMember(ITypeSymbol underlyingType)
        => UnderlyingMemberMatch(underlyingType.GetMembers(Name).OfType<T>()).FirstOrDefault();
    protected virtual IEnumerable<T> UnderlyingMemberMatch(IEnumerable<T> underlyingMembers)
        => underlyingMembers;
}