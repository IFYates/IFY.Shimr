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
        try
        {
            _ = handleShimMethodCall(context)
                || handleStaticShim(context);
        }
        catch (Exception ex)
        {
            var err = $"{ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}";
            Diag.WriteOutput($"// ERROR: {err}");
            // TODO: _errors.CodeGenFailed(ex);
        }
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
        var targetType = context.SemanticModel.GetTypeInfo(membAccessExpr.Expression).Type;
        if (targetType?.ToDisplayString() is null or "object")
        {
            Errors.NoTypeWarning(context.Node, targetType);
            return true;
        }

        // Register shim type
        Shims.GetOrCreateShim(argTypeInfo)
            .AddTarget(targetType);
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
        var interfaceAttr = interfaceDeclaration.GetAttribute<StaticShimAttribute>(context.SemanticModel);
        if (interfaceAttr != null)
        {
            Shims.GetOrCreateFactory(factoryType);
        }

        // Find StaticShimAttribute(Type) on members (currently only 1 per member)
        foreach (var member in interfaceDeclaration.Members.Where(m => m is PropertyDeclarationSyntax or MethodDeclarationSyntax))
        {
            var memberAttr = member.GetAttribute<StaticShimAttribute>(context.SemanticModel);
            if (memberAttr != null)
            {
                // Get type argument from attribute
                var typeArg = memberAttr.GetAttributeTypeParameter(context.SemanticModel);
                if (typeArg?.ToDisplayString() is null or "object")
                {
                    Errors.NoTypeWarning(context.Node, typeArg);
                    continue;
                }
                if (typeArg.TypeKind == TypeKind.Interface)
                {
                    Errors.InterfaceUseError(context.Node, typeArg);
                    continue;
                }

                var singleMember = context.SemanticModel.GetDeclaredSymbol(member)!;
                Shims.GetOrCreateFactory(factoryType)
                    .SetMemberType(singleMember, typeArg);
            }
        }

        // Find ConstructorShimAttribute(Type) on members (currently only 1 per member)
        foreach (var method in interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            var memberAttr = method.GetAttribute<ConstructorShimAttribute>(context.SemanticModel);
            if (memberAttr != null)
            {
                // Get type argument from member attribute or StaticShimAttribute on interface
                var typeArg = memberAttr.GetAttributeTypeParameter(context.SemanticModel)
                    ?? interfaceAttr?.GetAttributeTypeParameter(context.SemanticModel);
                if (typeArg?.ToDisplayString() is null or "object")
                {
                    Errors.NoTypeWarning(context.Node, typeArg);
                    continue;
                }
                if (typeArg.TypeKind == TypeKind.Interface)
                {
                    Errors.InterfaceUseError(context.Node, typeArg);
                    continue;
                }

                // TODO: move to member logic
                // Check return type is valid
                var returnType = context.SemanticModel.GetDeclaredSymbol(method)?.ReturnType;
                if (returnType == null || (!returnType.IsMatch(typeArg) && returnType.TypeKind != TypeKind.Interface))
                {
                    Errors.InvalidReturnTypeError(method.ReturnType, method.Identifier.Text /* TODO: signature */, returnType?.ToDisplayString() ?? "Unknown");
                    continue;
                }

                // Register shim factory
                var member = context.SemanticModel.GetDeclaredSymbol(method)!;
                Shims.GetOrCreateFactory(factoryType)
                    .SetMemberType(member, typeArg);
            }
        }

        return true;
    }
}
