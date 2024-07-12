using IFY.Shimr.CodeGen.Models;
using IFY.Shimr.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

/// <summary>
/// Finds all uses of '<see cref="ObjectExtensions"/>.Shim&lt;T&gt;(object)' extension method and '<see cref="ObjectExtensions"/>.Create&lt;T&gt;()'.
/// </summary>
internal class ShimResolver(CodeErrorReporter errors, ShimRegister shimRegister) : ISyntaxContextReceiver
{
    private static readonly string ShimExtensionType = typeof(ObjectExtensions).FullName;

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        // TODO: reset diag output on first of each run

        _ = handleShimMethodCall(context)
            || handleStaticShim(context);
    }

    // object.Shim<T>()
    private bool handleShimMethodCall(GeneratorSyntaxContext context)
    {
        // Only process Shim<T>() invocations
        if (context.Node is not InvocationExpressionSyntax invokeExpr
            || invokeExpr.Expression is not MemberAccessExpressionSyntax membAccessExpr
            || invokeExpr.ArgumentList.Arguments.Count > 0
            || membAccessExpr.Name is not GenericNameSyntax name
            || name.TypeArgumentList.Arguments.Count != 1
            || name.TypeArgumentList.Arguments[0] is not TypeSyntax argType
            || name.Identifier.ValueText != nameof(ObjectExtensions.Shim))
        {
            return false;
        }

        // Only look at reference to ShimBuilder or generated coded (null)
        var memberSymbolInfo = context.SemanticModel.GetSymbolInfo(membAccessExpr.Name);
        if (memberSymbolInfo.Symbol != null
            && memberSymbolInfo.Symbol.ContainingType.ToDisplayString() != ShimExtensionType)
        {
            return false;
        }

        // Arg type info
        var argTypeInfo = context.SemanticModel.GetTypeInfo(argType).Type;
        if (argTypeInfo?.TypeKind != TypeKind.Interface)
        {
            errors.NonInterfaceError(context.Node, argTypeInfo);
            return true;
        }

        // Underlying type info
        var shimdType = context.SemanticModel.GetTypeInfo(membAccessExpr.Expression).Type;
        if (shimdType?.ToDisplayString() is null or "object")
        {
            errors.NoTypeWarning(context.Node, shimdType);
            return true;
        }

        // Register shim
        shimRegister.GetOrCreate(argTypeInfo)
            .AddShim(shimdType);
        return true;
    }

    // StaticShimAttribute(Type)
    private bool handleStaticShim(GeneratorSyntaxContext context)
    {
        // Check every interface for attributes
        if (context.Node is not InterfaceDeclarationSyntax interfaceDeclaration
            || !interfaceDeclaration.AttributeLists.Any())
        {
            return false;
        }

        // Only interested in StaticShimAttribute(Type)
        var staticShimAttrSymbol = context.SemanticModel.Compilation
            .GetTypeByMetadataName(typeof(StaticShimAttribute).FullName)!;
        var attrs = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => context.SemanticModel.GetTypeInfo(a).Type?.IsMatch(staticShimAttrSymbol) == true)
            .ToArray();
        if (attrs.Length != 1) // Currently only allow single use
        {
            return attrs.Length > 0;
        }

        // Get type argument from constructor
        var nodes = attrs[0].ChildNodes().ToArray();
        if (nodes.Length != 2
            || nodes[1] is not AttributeArgumentListSyntax argList
            || argList.Arguments.Count != 1
            || argList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOf)
        {
            return true;
        }
        var typeArg = context.SemanticModel.GetTypeInfo(typeOf.Type).Type;
        if (typeArg?.ToDisplayString() is null or "object")
        {
            errors.NoTypeWarning(context.Node, typeArg);
            return true;
        }
        if (typeArg.TypeKind == TypeKind.Interface)
        {
            errors.InterfaceUseError(context.Node, typeArg);
            return true;
        }

        // Factory type
        var factoryType = context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration)!;

        // Register shim factory
        shimRegister.GetOrCreate(factoryType)
            .AddShimFactory(typeArg);
        return true;
    }
}

// TODO: And from attributes:
// - TypeShim
// - auto return
// - ...