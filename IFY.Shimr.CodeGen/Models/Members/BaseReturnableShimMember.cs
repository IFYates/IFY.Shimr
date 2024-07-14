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
    public string OriginalName { get; } = symbol.GetShimName() ?? symbol.Name;
    public abstract ITypeSymbol ReturnType { get; }
    public abstract string ReturnTypeName { get; }

    public void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType)
    {
        var underlyingMember = GetUnderlyingMember(underlyingType);
        GenerateCode(code, errors, underlyingType, underlyingMember);
    }
    public abstract void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType, T? underlyingMember);

    public string GetMemberCallee(ITypeSymbol type, T member)
    {
        var callee = "_inst";
        if (member.IsStatic)
        {
            callee = type.ToDisplayString();
        }
        else if (!member.ContainingType.IsMatch(type))
        {
            callee = $"(({member.ContainingType.ToDisplayString()}){callee})";
        }
        return callee;
    }
    public string? GetShimCode(ITypeSymbol underlyingReturnType)
    {
        string? str = null;// $"/* {underlyingReturnType.ToDisplayString()} */";
        if (ReturnType.IsMatch(underlyingReturnType))
        {
            return str;
        }

        // Array shim
        if (ReturnType is IArrayTypeSymbol returnArrayType)
        {
            var elementTypeName = returnArrayType.ElementType.ToDisplayString();
            return $"{str}.Select(e => e.Shim<{elementTypeName}>()).ToArray()";
        }

        // IEnumerable<> shim
        // TODO: Any implementation
        if (ReturnType is INamedTypeSymbol returnType
            && returnType.Name == nameof(System.Collections.IEnumerable)
            && returnType.TypeArguments.Length == 1)
        {
            var elementTypeName = returnType.TypeArguments[0].ToDisplayString();
            return $"{str}.Select(e => e.Shim<{elementTypeName}>())";
        }

        // Only shim if it's an interface
        return ReturnType.TypeKind != TypeKind.Interface ? str : $"{str}.Shim<{ReturnTypeName}>()";
    }
    public string? GetUnshimCode(ITypeSymbol underlyingReturnType)
    {
        var doShim = !ReturnType.IsMatch(underlyingReturnType);
        // TODO: check if shim registered

        return !doShim ? string.Empty : $".Unshim<{underlyingReturnType.ToDisplayString()}>()";
    }

    public T? GetUnderlyingMember(ITypeSymbol underlyingType)
    {
        var members = underlyingType.GetAllMembers().OfType<T>()
            .Where(m => m.Name == OriginalName)
            .Where(IsUnderlyingMemberMatch)
            .OrderByDescending(m => GetMemberReturn(m)!.IsMatch(ReturnType)).ToArray();
        return members.FirstOrDefault();
    }
    protected virtual bool IsUnderlyingMemberMatch(T member)
        => true;

    public abstract ITypeSymbol? GetMemberReturn(T? member);

    public void ResolveImplicitShims(ShimRegister shimRegister, IShimTarget target)
    {
        // Return types
        var underlyingReturn = GetMemberReturn(GetUnderlyingMember(target.UnderlyingType)) ?? ReturnType;
        if (!underlyingReturn.IsMatch(ReturnType))
        {
            // Arrays
            if (ReturnType is IArrayTypeSymbol returnArrayType
                && underlyingReturn is IArrayTypeSymbol underlyingArrayType)
            {
                shimRegister.GetOrCreate(returnArrayType.ElementType)
                    .AddShim(underlyingArrayType.ElementType);
                return;
            }

            // IEnumerable<>
            // TODO: Any implementation
            if (ReturnType is INamedTypeSymbol returnType && underlyingReturn is INamedTypeSymbol underlyingType
                && returnType.Name == nameof(System.Collections.IEnumerable) && underlyingType.Name == nameof(System.Collections.IEnumerable)
                && returnType.TypeArguments.Length == 1 && underlyingType.TypeArguments.Length == 1)
            {
                if (returnType.TypeArguments[0].TypeKind == TypeKind.Interface)
                {
                    shimRegister.GetOrCreate(returnType.TypeArguments[0])
                        .AddShim(underlyingType.TypeArguments[0]);
                }
                return;
            }

            if (ReturnType.TypeKind == TypeKind.Interface)
            {
                shimRegister.GetOrCreate(ReturnType)
                    .AddShim(underlyingReturn);
            }
        }

        DoResolveImplicitShims(shimRegister, target);
    }
    protected virtual void DoResolveImplicitShims(ShimRegister shimRegister, IShimTarget target) { }
}