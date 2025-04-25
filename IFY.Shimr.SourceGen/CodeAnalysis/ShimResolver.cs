using IFY.Shimr.SourceGen.Models;
using IFY.Shimr.SourceGen.Models.Bindings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.SourceGen.CodeAnalysis;

/// <summary>
/// Finds all uses of 'ObjectExtensions.Shim&lt;T&gt;(object)' extension method and 'ObjectExtensions.Create&lt;T&gt;()'.
/// </summary>
internal class ShimResolver : ISyntaxContextReceiver
{
    // TODO: only generate each shim once

    // Identify which syntax nodes require code generation
    public bool ShouldProcess(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is MemberAccessExpressionSyntax;
    }

    // Convert syntax nodes to generatable data
    public IBinding Process(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var n = (Delegate)((MemberAccessExpressionSyntax)context.Node).Kind;
        var y = ((GenericNameSyntax)((MemberAccessExpressionSyntax)n.Target).Name).Identifier.Value;
        return null;
    }




    private readonly Dictionary<string, IShimDefinition> _pool = [];
    public IEnumerable<IShimDefinition> Definitions => _pool.Values;

    // TODO: public CodeErrorReporter Errors { get; } = new();

    public IShimDefinition GetOrCreate(ITypeSymbol interfaceType, bool asFactory)
    {
        lock (_pool)
        {
            var key = interfaceType.ToDisplayString();
            if (!_pool.TryGetValue(key, out var shim))
            {
                shim = !asFactory
                    ? new InstanceShimDefinition(interfaceType)
                    : new ShimFactoryDefinition(interfaceType);
                _pool.Add(key, shim);
            }
            if (!asFactory && shim is not InstanceShimDefinition)
            {
                Diag.WriteOutput("// Got factory as instance shim: " + interfaceType.ToDisplayString());
            }
            return shim;
        }
    }
    public InstanceShimDefinition GetOrCreateShim(ITypeSymbol interfaceType)
        => (InstanceShimDefinition)GetOrCreate(interfaceType, false);
    public ShimFactoryDefinition GetOrCreateFactory(ITypeSymbol interfaceType)
        => (ShimFactoryDefinition)GetOrCreate(interfaceType, true);

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
            // TODO: Errors.CodeGenError(ex);
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
            || name.Identifier.ValueText != "Shim")
        {
            return false;
        }

        // Only look at reference to ShimBuilder or generated coded (null)
        var memberSymbolInfo = context.SemanticModel.GetSymbolInfo(membAccessExpr.Name);
        if (memberSymbolInfo.Symbol != null
            && memberSymbolInfo.Symbol.ContainingType.ToDisplayString() != GlobalCodeWriter.EXT_CLASSNAMEFULL)
        {
            return false;
        }

        // Arg type info
        var argTypeInfo = context.SemanticModel.GetTypeInfo(argType).Type;
        if (argTypeInfo?.TypeKind != TypeKind.Interface)
        {
            // TODO: Errors.NonInterfaceError(context.Node, argTypeInfo);
            return true;
        }

        // Underlying type info
        var targetType = context.SemanticModel.GetTypeInfo(membAccessExpr.Expression).Type;
        if (targetType?.ToDisplayString() is null or "object")
        {
            // TODO: Errors.NoTypeWarning(context.Node, targetType);
            return true;
        }

        // Register shim type
        GetOrCreateShim(argTypeInfo)
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
            GetOrCreateFactory(factoryType);
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
                    // TODO: Errors.NoTypeWarning(context.Node, typeArg);
                    continue;
                }
                if (typeArg.TypeKind == TypeKind.Interface)
                {
                    // TODO: Errors.InterfaceUseError(context.Node, typeArg);
                    continue;
                }

                var singleMember = context.SemanticModel.GetDeclaredSymbol(member)!;
                GetOrCreateFactory(factoryType)
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
                    // TODO: Errors.NoTypeWarning(context.Node, typeArg);
                    continue;
                }
                if (typeArg.TypeKind == TypeKind.Interface)
                {
                    // TODO: Errors.InterfaceUseError(context.Node, typeArg);
                    continue;
                }

                // Check return type is valid
                var member = context.SemanticModel.GetDeclaredSymbol(method);
                if (member?.ReturnType == null || (!member.ReturnType.IsMatch(typeArg) && member.ReturnType.TypeKind != TypeKind.Interface))
                {
                    // TODO: Errors.InvalidReturnTypeError(method.ReturnType, method.Identifier.Text /* TODO: full signature */, member?.ReturnType?.ToDisplayString() ?? "Unknown");
                    continue;
                }

                // Register shim factory
                GetOrCreateFactory(factoryType)
                    .SetMemberType(member, typeArg);
            }
        }

        return true;
    }

    /// <summary>
    /// Ensure that all implicit shims in registered shims are resolved.
    /// </summary>
    /// <returns>All current shims.</returns>
    public IList<IBinding> ResolveAllShims()
    {
        var bindings = new List<IBinding>();
        var shimsDone = new List<IShimDefinition>();
        var newShims = _pool.Values.Except(shimsDone).ToArray();
        while (newShims.Any())
        {
            foreach (var shimType in newShims)
            {
                shimType.Resolve(bindings, this);
            }
            shimsDone.AddRange(newShims);
            newShims = _pool.Values.Except(shimsDone).ToArray();
        }

        return bindings;
    }
}
