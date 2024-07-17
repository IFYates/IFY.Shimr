using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

internal static class SyntaxHelpers
{
    public static StringBuilder AppendLine(this StringBuilder sb, int indent, string value)
        => sb.Append(' ', indent * 4).AppendLine(value);

    public static bool AllParameterTypesMatch(this IMethodSymbol method, IEnumerable<IParameterSymbol> parameters)
    {
        // TODO: out, ref
        var parameterArray = parameters.ToArray();
        return method.Parameters.Length == parameterArray.Length
            && method.Parameters.Select(isParameterMatch).All(v => v);
        bool isParameterMatch(IParameterSymbol param1, int idx)
            => param1.GetArgumentShimType().IsMatch(parameterArray[idx].GetArgumentShimType());
    }

    /// <summary>
    /// Get all the members of this symbol, including those from base type.
    /// </summary>
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol typeSymbol && typeSymbol.IsUnboundGenericType)
        {
            symbol = typeSymbol.ConstructedFrom;
        }

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

    public static string Hash(this string input)
    {
        using var md5 = MD5.Create();
        var buffer = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(buffer);
        return string.Concat(hash.Select(b => b.ToString("X2")));
    }

    /// <summary>
    /// Are <paramref name="type1"/> and <paramref name="type2"/> referring to the same type.
    /// </summary>
    public static bool IsMatch(this ITypeSymbol type1, ITypeSymbol? type2)
        => (type1 is ITypeParameterSymbol && type2 is ITypeParameterSymbol) // TODO: Is this enough?
        || type1.Equals(type2, SymbolEqualityComparer.Default)
        || type1.ToFullName() == type2?.ToFullName(); // HACK: System.Collections.Generic.IDictionary<T1, T2> comparison failing
    /// <summary>
    /// Can <paramref name="type1"/> be used to refer to the use of <paramref name="type2"/>.
    /// This includes inheritiance.
    /// </summary>
    public static bool IsMatchable(this ITypeSymbol type1, ITypeSymbol? type2)
        => type1.IsMatch(type2)
        || (type1.TypeKind == TypeKind.Interface && type2?.AllInterfaces.Any(type1.IsMatch) == true)
        || (type2?.BaseType != null && type1.IsMatch(type2.BaseType));

    public static bool IsType<T>(this ITypeSymbol symbol)
        => symbol.ToFullName() == typeof(T).FullName;

    private static readonly SymbolDisplayFormat _displayFormatFull = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );
    private static readonly SymbolDisplayFormat _displayFormatGeneric = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );
    public static string ToFullName(this ITypeSymbol type)
        => type.ToDisplayString(_displayFormatFull);
    public static string ToGenericName(this INamedTypeSymbol type)
        => type.ToDisplayString(_displayFormatGeneric) + (type.IsGenericType ? "<>" : null);

    public static string ToTypeParameterList(this IEnumerable<ITypeParameterSymbol> typeParameters, bool withAngles = true)
    {
        var args = string.Join(", ", typeParameters.Select(p => p.Name + (p.NullableAnnotation == NullableAnnotation.Annotated ? "?" : null)));
        return withAngles ? $"<{args}>" : args;
    }
    public static string ToWhereClause(this IEnumerable<ITypeParameterSymbol> typeParameters)
    {
        // TODO: SymbolDisplayGenericsOptions.IncludeTypeConstraints exists
        var sb = new StringBuilder();
        foreach (var parameter in typeParameters)
        {
            var p = new StringBuilder();
            if (parameter.HasConstructorConstraint)
            {
                p.Append(", new()");
            }
            if (parameter.HasReferenceTypeConstraint)
            {
                p.Append(", class");
            }
            if (parameter.HasValueTypeConstraint)
            {
                p.Append(", struct");
            }
            if (parameter.HasNotNullConstraint)
            {
                p.Append(", notnull");
            }
            foreach (var classConstraint in parameter.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Class))
            {
                p.Append(", ").Append(classConstraint.ToFullName());
            }
            foreach (var interfaceConstraint in parameter.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Interface))
            {
                p.Append(", ").Append(interfaceConstraint.ToFullName());
            }
            if (p.Length > 2)
            {
                sb.Append($" where {parameter.Name} : {p.Remove(0, 2)}");
            }
        }
        return sb.ToString();
    }
}
