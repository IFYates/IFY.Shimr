using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    /// <summary>
    /// Get the 'Type' that is represented by a 'typeof()' constant on the only constructor argument of <param name="node"/>.
    /// </summary>
    /// <Example>[Attribute(typeof(T))]</Example>
    public static ITypeSymbol? GetConstructorTypeofArgument(this SyntaxNode node, SemanticModel semanticModel)
    {
        var nodes = node.ChildNodes().ToArray();
        if (nodes.Length != 2
            || nodes[1] is not AttributeArgumentListSyntax argList
            || argList.Arguments.Count != 1
            || argList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOf)
        {
            return null;
        }
        return semanticModel.GetTypeInfo(typeOf.Type).Type;
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
