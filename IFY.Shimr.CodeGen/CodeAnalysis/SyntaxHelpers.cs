using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

internal static class SyntaxHelpers
{
    public static bool AllParameterTypesMatch(this IMethodSymbol method1, IMethodSymbol method2)
    {
        // TODO: out, ref
        return method1.Parameters.Length == method2.Parameters.Length
            && method1.Parameters.Select(isParameterMatch).All(v => v);
        bool isParameterMatch(IParameterSymbol param1, int idx)
            => param1.GetArgumentShimType().IsMatch(method2.Parameters[idx].GetArgumentShimType());
    }

    /// <summary>
    /// Get all the members of this symbol, including those from base type.
    /// </summary>
    public static ISymbol[] GetAllMembers(this ITypeSymbol symbol)
    {
        var members = symbol.GetMembers().ToList();
        while (symbol.BaseType != null)
        {
            symbol = symbol.BaseType;
            members.AddRange(symbol.GetMembers());
        }
        return members.Distinct(SymbolEqualityComparer.Default).ToArray();
    }

    public static ITypeSymbol GetArgumentShimType(this IParameterSymbol arg, bool nullIfNoOverride = false)
    {
        // Look for TypeShimAttribute(Type) and return constructor arg
        var attr = arg.GetAttributes().SingleOrDefault(a => a.AttributeClass?.IsType<TypeShimAttribute>() == true);
        if (attr != null)
        {
            return (ITypeSymbol)attr.ConstructorArguments.Single().Value!;
        }
        return nullIfNoOverride ? null! : arg.Type;
    }

    public static bool IsMatch(this ITypeSymbol type1, ITypeSymbol type2)
        => type1.Equals(type2, SymbolEqualityComparer.Default);

    public static bool IsType<T>(this INamedTypeSymbol symbol)
    {
        var symbolFullName = symbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        var typeFullName = typeof(T).FullName;
        return symbolFullName == typeFullName || symbolFullName == $"global::{typeFullName}";
    }
}
