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

    public static AttributeData? GetAttribute<T>(this ISymbol symbol)
        where T : Attribute
    {
        return symbol?.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.IsType<T>() == true);
    }
    public static AttributeSyntax? GetAttribute<T>(this MemberDeclarationSyntax member, SemanticModel semanticModel)
        where T : Attribute
    {
        if (!_attributeTypeSymbol.TryGetValue(typeof(T), out var attrSymbol))
        {
            attrSymbol = semanticModel.Compilation.GetTypeByMetadataName(typeof(T).FullName)!;
            _attributeTypeSymbol[typeof(T)] = attrSymbol;
        }
        return member.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => semanticModel.GetTypeInfo(a).Type?.IsMatch(attrSymbol) == true)
            .SingleOrDefault();
    }
    private static readonly Dictionary<Type, ITypeSymbol> _attributeTypeSymbol = [];

    /// <summary>
    /// Get the 'Type' that is represented by a 'typeof()' constant on the only constructor argument of <param name="attr"/>.
    /// </summary>
    /// <Example>[Attribute(typeof(T))]</Example>
    public static ITypeSymbol? GetAttributeTypeParameter(this AttributeSyntax attr, SemanticModel semanticModel)
    {
        var nodes = attr.ChildNodes().ToArray();
        if (nodes.Length != 2
            || nodes[1] is not AttributeArgumentListSyntax argList
            || argList.Arguments.Count != 1
            || argList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOf)
        {
            return null;
        }
        return semanticModel.GetTypeInfo(typeOf.Type).Type;
    }

    public static SyntaxNode? GetSyntaxNode(this ISymbol symbol)
        => symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

    public static bool IsEnumerable(this ITypeSymbol type, out ITypeSymbol? elementType)
    {
        // Array shim
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        // IEnumerable<> shim
        var ienum = type.AllInterfaces.Add((INamedTypeSymbol)type)
            .Where(i => i.TypeKind == TypeKind.Interface && i.IsGenericType && i.TypeArguments.Length == 1)
            .FirstOrDefault(i => i.Name == nameof(System.Collections.IEnumerable));
        if (ienum != null)
        {
            elementType = ienum.TypeArguments[0];
            return true;
        }

        elementType = null;
        return false;
    }

    public static bool IsMatch(this ITypeSymbol type1, ITypeSymbol? type2)
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
}
