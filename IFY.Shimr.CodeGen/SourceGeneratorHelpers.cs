using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.CodeGen;

internal static class SourceGeneratorHelpers
{
    const char NAMESPACE_CLASS_DELIMITER = '.';
    //const char NESTED_CLASS_DELIMITER = '+';
    public static string GetFullName(this TypeDeclarationSyntax source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var fullName = new StringBuilder(source.Identifier.Text);
        for (var parent = source.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is BaseTypeDeclarationSyntax type)
            {
                fullName.Insert(0, type.Identifier.Text + NAMESPACE_CLASS_DELIMITER);
            }
        }
        for (var parent = source.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is BaseNamespaceDeclarationSyntax nsDeclaration)
            {
                fullName.Insert(0, nsDeclaration.Name.ToString() + NAMESPACE_CLASS_DELIMITER);
                break;
            }
        }

        return fullName.ToString();
    }

    public static INamedTypeSymbol? GetTypeSymbol(this AttributeSyntax syntax, GeneratorExecutionContext context)
        => GetTypeSymbol(syntax, context.Compilation);
    public static INamedTypeSymbol? GetTypeSymbol(this AttributeSyntax syntax, Compilation compilation)
    {
        if (syntax is null)
        {
            return null;
        }
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        var symbolInfo = semanticModel.GetSymbolInfo(syntax);
        return ((IMethodSymbol)symbolInfo.Symbol!).ContainingType;
    }

    public static INamedTypeSymbol? GetTypeSymbol(this BaseTypeDeclarationSyntax syntax, GeneratorExecutionContext context)
        => GetTypeSymbol(syntax, context.Compilation);
    public static INamedTypeSymbol? GetTypeSymbol(this BaseTypeDeclarationSyntax syntax, Compilation compilation)
    {
        if (syntax is null)
        {
            return null;
        }
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        return (INamedTypeSymbol?)semanticModel.GetDeclaredSymbol(syntax);
    }

    public static INamedTypeSymbol? GetTypeSymbol(this TypeSyntax syntax, GeneratorExecutionContext context)
        => GetTypeSymbol(syntax, context.Compilation);
    public static INamedTypeSymbol? GetTypeSymbol(this TypeSyntax syntax, Compilation compilation)
    {
        if (syntax is null)
        {
            return null;
        }
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        var symbolInfo = semanticModel.GetSymbolInfo(syntax);
        return (INamedTypeSymbol?)symbolInfo.Symbol;
    }

    public static string GetParametersSignature(this IMethodSymbol symbol)
    {
        return string.Join(", ", symbol.Parameters.Select(getParameterSignature));
        static string getParameterSignature(IParameterSymbol symbol)
        {
            // TODO: out, ref, default
            // TODO: attributes?
            return $"{symbol.Type.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat)} {symbol.Name}";
        }
    }

    public static bool AllParameterTypesMatch(this IMethodSymbol method1, IMethodSymbol method2)
    {
        // TODO: out, ref
        return method1.Parameters.Length == method2.Parameters.Length
            && method1.Parameters.Select(isParameterMatch).All(v => v);
        bool isParameterMatch(IParameterSymbol param1, int idx)
            => param1.Type.Equals(method2.Parameters[idx].Type, SymbolEqualityComparer.Default);
    }
}
