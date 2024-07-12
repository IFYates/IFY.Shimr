using IFY.Shimr.CodeGen.Models;
using IFY.Shimr.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

/// <summary>
/// Finds all uses of '<see cref="ObjectExtensions"/>.Shim&lt;T&gt;(object)' extension method and '<see cref="ObjectExtensions"/>.Create&lt;T&gt;()'.
/// </summary>
internal class ShimResolver : ISyntaxContextReceiver
{
    private static readonly string ShimExtensionType = typeof(ObjectExtensions).FullName;

    public CodeErrorReporter Errors { get; } = new();
    public ShimRegister Shims { get; } = new();

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
            Errors.NonInterfaceError(context.Node, argTypeInfo);
            return true;
        }

        // Underlying type info
        var shimdType = context.SemanticModel.GetTypeInfo(membAccessExpr.Expression).Type;
        if (shimdType?.ToDisplayString() is null or "object")
        {
            Errors.NoTypeWarning(context.Node, shimdType);
            return true;
        }

        // Register shim
        Shims.GetOrCreate(argTypeInfo)
            .AddShim(shimdType);
        return true;
    }

    // StaticShimAttribute(Type)
    private bool handleStaticShim(GeneratorSyntaxContext context)
    {
        // Check every interface for direct attributes or attributes on members
        if (context.Node is not InterfaceDeclarationSyntax interfaceDeclaration)
        {
            return false;
        }

        // Factory interface type
        var factoryType = context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration)!;

        // Get StaticShimAttribute(Type) on interface (currently only 1)
        var staticShimAttrSymbol = context.SemanticModel.Compilation
            .GetTypeByMetadataName(typeof(StaticShimAttribute).FullName)!;
        var interfaceAttr = interfaceDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => context.SemanticModel.GetTypeInfo(a).Type?.IsMatch(staticShimAttrSymbol) == true)
            .SingleOrDefault();
        if (interfaceAttr != null)
        {
            registerFactory(interfaceAttr, interfaceDeclaration);
        }

        // Find attributes on members (currently only 1 per member)
        foreach (var memberDeclaration in interfaceDeclaration.Members)
        {
            var memberAttr = memberDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(a => context.SemanticModel.GetTypeInfo(a).Type?.IsMatch(staticShimAttrSymbol) == true)
                .SingleOrDefault();
            if (memberAttr != null)
            {
                registerFactory(memberAttr, memberDeclaration);
            }
        }

        return true;

        void registerFactory(AttributeSyntax attr, SyntaxNode parent)
        {
            // Get type argument from attribute constructor
            var nodes = attr.ChildNodes().ToArray();
            if (nodes.Length != 2
                || nodes[1] is not AttributeArgumentListSyntax argList
                || argList.Arguments.Count != 1
                || argList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOf)
            {
                return;
            }
            var typeArg = context.SemanticModel.GetTypeInfo(typeOf.Type).Type;
            if (typeArg?.ToDisplayString() is null or "object")
            {
                Errors.NoTypeWarning(context.Node, typeArg);
                return;
            }
            if (typeArg.TypeKind == TypeKind.Interface)
            {
                Errors.InterfaceUseError(context.Node, typeArg);
                return;
            }

            // May be for single member
            ISymbol? singleMember = null;
            if (parent is not InterfaceDeclarationSyntax)
            {
                singleMember = context.SemanticModel.GetDeclaredSymbol(parent);
            }

            // Register shim factory
            Shims.GetOrCreateFactory(factoryType)
                .AddShim(typeArg, singleMember);
        }
    }
}
