using IFY.Shimr.CodeGen.Models;
using IFY.Shimr.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

// TODO: refactor so that output structure is "fully" registered here (some information may not available yet)
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
            registerFactory(interfaceAttr, null);
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

        // Find attribute on members (currently only 1 per member)
        var constructorShimAttrSymbol = context.SemanticModel.Compilation
            .GetTypeByMetadataName(typeof(ConstructorShimAttribute).FullName)!;
        foreach (var methodDeclaration in interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            var memberAttr = methodDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(a => context.SemanticModel.GetTypeInfo(a).Type?.IsMatch(constructorShimAttrSymbol) == true)
                .SingleOrDefault();
            if (memberAttr != null)
            {
                registerConstructor(memberAttr, methodDeclaration);
            }
        }

        return false; // Don't stop

        void registerFactory(AttributeSyntax attr, SyntaxNode? member)
        {
            // Get type argument from attribute constructor
            var typeArg = attr.GetConstructorTypeofArgument(context.SemanticModel);
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
            if (member != null)
            {
                singleMember = context.SemanticModel.GetDeclaredSymbol(member);
            }

            // Register shim factory
            Shims.GetOrCreateFactory(factoryType)
                .AddShim(typeArg, singleMember);
        }

        void registerConstructor(AttributeSyntax attr, MethodDeclarationSyntax method)
        {
            // Get type argument from attribute constructor
            var typeArg = attr.GetConstructorTypeofArgument(context.SemanticModel);
            if (typeArg == null)
            {
                // Resolve type from StaticShimAttribute on interface
                typeArg = interfaceAttr?.GetConstructorTypeofArgument(context.SemanticModel);
            }
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

            // Check return type is valid
            var returnType = context.SemanticModel.GetDeclaredSymbol(method)?.ReturnType;
            if (returnType == null || (!returnType.IsMatch(typeArg) && returnType.TypeKind != TypeKind.Interface))
            {
                Errors.InvalidReturnTypeError(method.ReturnType, method.Identifier.Text /* TODO: signature */, returnType?.ToDisplayString() ?? "Unknown");
                return;
            }

            // Register shim factory
            var member = context.SemanticModel.GetDeclaredSymbol(method);
            Shims.GetOrCreateFactory(factoryType)
                .AddConstructor(typeArg, member!);
        }
    }
}
