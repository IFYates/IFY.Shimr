using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        if (symbol.BaseType != null)
        {
            members.AddRange(symbol.BaseType.GetAllMembers());
        }
        foreach (var iface in symbol.AllInterfaces)
        {
            members.AddRange(iface.GetMembers());
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

    public static string? GetShimName(this ISymbol symbol)
    {
        var attr = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.IsType<ShimAttribute>() == true);
        Diag.WriteOutput($"//// {symbol.ToDisplayString()} // {attr?.ConstructorArguments[0].Type} // {attr?.ConstructorArguments[0].Type!.IsType<string>()}");
        //if (symbol.Name == "Value")
        //{
        //    Diag.Debug();
        //}
        // Could be (string), (Type), or (Type, string)
        return (attr?.ConstructorArguments.Length) switch
        {
            1 => attr.ConstructorArguments[0].Type!.IsType<string>()
                ? attr.ConstructorArguments[0].Value?.ToString() : null,
            2 => attr.ConstructorArguments[1].Value?.ToString(),
            _ => null,
        };
    }

    public static bool IsMatch(this ITypeSymbol type1, ITypeSymbol type2)
        => type1.Equals(type2, SymbolEqualityComparer.Default);

    public static bool IsType<T>(this ITypeSymbol symbol)
        => symbol.ToFullName() == typeof(T).FullName;

    private static readonly SymbolDisplayFormat _displayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );
    public static string ToFullName(this ITypeSymbol type)
        => type.ToDisplayString(_displayFormat);

    public static string ToUniqueName(this ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol ms => $"{ms.Name}__{string.Join("_", ms.Parameters.Select(p => p.Type.Name))}",
            // TODO: Others
            _ => symbol.Name,
        };
    }
}
