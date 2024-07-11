using IFY.Shimr.CodeGen.Models;
using IFY.Shimr.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

/// <summary>
/// Finds all uses of '<see cref="ShimBuilder"/>.Shim&lt;T&gt;(object)' extension method and '<see cref="ShimBuilder"/>.Create&lt;T&gt;()'.
/// </summary>
internal class ShimResolver(CodeError errors, ShimRegister shimRegister) : ISyntaxContextReceiver
{
    private static readonly string ShimBuilderType = typeof(ShimBuilder).FullName;

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        // TODO: reset diag output on first of each run

        _ = handleShimMethodCall(context)
            || handleShimFactoryCreate(context);
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
            || name.Identifier.ValueText != nameof(ShimBuilder.Shim))
        {
            return false;
        }

        // Only look at reference to ShimBuilder or generated coded (null)
        var memberSymbolInfo = context.SemanticModel.GetSymbolInfo(membAccessExpr.Name);
        if (memberSymbolInfo.Symbol != null
            && memberSymbolInfo.Symbol.ContainingType.ToDisplayString() != ShimBuilderType)
        {
            return false;
        }

        // Arg type info
        var argTypeInfo = context.SemanticModel.GetTypeInfo(argType);
        if (argTypeInfo.Type?.TypeKind != TypeKind.Interface)
        {
            errors.NonInterfaceError(context.Node, argTypeInfo.Type);
            return true;
        }

        // Underlying type info
        var shimdTypeInfo = context.SemanticModel.GetTypeInfo(membAccessExpr.Expression);
        if (shimdTypeInfo.Type?.ToDisplayString() is null or "object")
        {
            errors.NoTypeWarning(context.Node, shimdTypeInfo.Type);
            return true;
        }

        // Register shim
        shimRegister.GetOrCreate((INamedTypeSymbol)argTypeInfo.Type)
            .AddShim((INamedTypeSymbol)shimdTypeInfo.Type);
        return true;
    }

    // ShimBuidler.Create<T>()
    private bool handleShimFactoryCreate(GeneratorSyntaxContext context)
    {
        // Only process ShimBuidler.Create<T>() invocations
        if (context.Node is not InvocationExpressionSyntax invokeExpr
            || invokeExpr.Expression is not MemberAccessExpressionSyntax membAccessExpr
            || invokeExpr.ArgumentList.Arguments.Count > 0
            || membAccessExpr.Name is not GenericNameSyntax name
            || name.TypeArgumentList.Arguments.Count != 1
            || name.TypeArgumentList.Arguments[0] is not TypeSyntax argType
            || name.Identifier.ValueText != nameof(ShimBuilder.Create))
        {
            return false;
        }

        // Only look at reference to ShimBuilder
        var memberSymbolInfo = context.SemanticModel.GetSymbolInfo(membAccessExpr.Name);
        if (memberSymbolInfo.Symbol?.ContainingType.ToDisplayString() != ShimBuilderType)
        {
            return false;
        }

        // Arg type info
        var argTypeInfo = context.SemanticModel.GetTypeInfo(argType);
        if (argTypeInfo.Type?.TypeKind != TypeKind.Interface)
        {
            errors.NonInterfaceError(context.Node, argTypeInfo.Type);
            return true;
        }

        // TODO: Find required attribute on interface

        // Underlying type info
        var shimdTypeInfo = context.SemanticModel.GetTypeInfo(membAccessExpr.Expression);
        if (shimdTypeInfo.Type?.ToDisplayString() is null or "object")
        {
            errors.NoTypeWarning(context.Node, shimdTypeInfo.Type);
            return true;
        }

        // Register shim factory
        shimRegister.GetOrCreate((INamedTypeSymbol)argTypeInfo.Type)
            .AddShim((INamedTypeSymbol)shimdTypeInfo.Type);
        return true;
    }
}

// TODO: And from attributes:
// - TypeShim
// - auto return
// - ...