using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal abstract class BaseReturnableShimMember<T>(BaseShimType baseShimType, T symbol)
    : IShimMember, IReturnableShimMember
    where T : ISymbol
{
    public BaseShimType BaseShimType { get; } = baseShimType;
    public T Symbol { get; } = symbol;
    ISymbol IShimMember.Symbol { get; } = symbol;
    public string Name { get; } = symbol.Name;
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
        => UnderlyingMemberMatch(underlyingType.GetAllMembers().OfType<T>().Where(m => m.Name == Name)).FirstOrDefault();
    protected virtual IEnumerable<T> UnderlyingMemberMatch(IEnumerable<T> underlyingMembers)
        => underlyingMembers;

    public void ResolveImplicitShims(ShimRegister shimRegister, IShimTarget target)
    {
        // Return types
        var underlyingReturn = GetUnderlyingMemberReturn(target.UnderlyingType);
        if (!underlyingReturn.IsMatch(ReturnType)
            && ReturnType.TypeKind == TypeKind.Interface)
        {
            shimRegister.GetOrCreate(ReturnType)
                .AddShim(underlyingReturn);
        }

        DoResolveImplicitShims(shimRegister, target);
    }
    protected virtual void DoResolveImplicitShims(ShimRegister shimRegister, IShimTarget target) { }
}