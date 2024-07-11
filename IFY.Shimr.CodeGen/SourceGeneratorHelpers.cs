using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.CodeGen;

internal static class SourceGeneratorHelpers
{
    public static bool IsType<T>(this INamedTypeSymbol symbol)
    {
        var symbolFullName = symbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        var typeFullName = typeof(T).FullName;
        return symbolFullName == typeFullName || symbolFullName == $"global::{typeFullName}";
    }

    public static bool AllParameterTypesMatch(this IMethodSymbol method1, IMethodSymbol method2)
    {
        // TODO: out, ref
        return method1.Parameters.Length == method2.Parameters.Length
            && method1.Parameters.Select(isParameterMatch).All(v => v);
        bool isParameterMatch(IParameterSymbol param1, int idx)
            => param1.GetArgumentShimType().IsMatch(method2.Parameters[idx].GetArgumentShimType());
    }

    public static bool IsMatch(this ITypeSymbol type1, ITypeSymbol type2)
        => type1.Equals(type2, SymbolEqualityComparer.Default);
}
