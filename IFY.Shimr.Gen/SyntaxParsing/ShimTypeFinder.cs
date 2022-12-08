using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Not sure how to test without unnecessary restructure
internal class ShimTypeFinder : ISyntaxContextReceiver
{
    public Exception? Exception { get; private set; }

    public List<ShimTypeDefinition> ShimTypes { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        try
        {
            // Only interested in certain interfaces
            if (context.Node is InterfaceDeclarationSyntax)
            {
                var interfaceDef = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!;

                // Add each shimmed type to list
                var shimrAttrs = interfaceDef.GetAttributes()
                    .Where(a => a.AttributeClass.FullName().StartsWith(typeof(ShimOfAttribute).FullName)).ToArray();
                foreach (var attr in shimrAttrs)
                {
                    if (attr.AttributeClass.TypeArguments.Length == 1)
                    {
                        ShimTypes.Add(new ShimTypeDefinition(new(interfaceDef), new((INamedTypeSymbol)attr.AttributeClass.TypeArguments[0]), false));
                    }
                    else if (attr.TryGetAttributeConstructorValue("targetType", out var targetType))
                    {
                        ShimTypes.Add(new ShimTypeDefinition(new(interfaceDef), new((INamedTypeSymbol)targetType!), false));
                    }
                }

                // Add each static shimmed type to list
                var staticAttr = interfaceDef.GetAttribute<StaticShimAttribute>();
                if (staticAttr?.TryGetAttributeConstructorValue("targetType", out var staticTargetType) == true)
                {
                    ShimTypes.Add(new ShimTypeDefinition(new(interfaceDef), new((INamedTypeSymbol)staticTargetType!), true));
                }
            }
        }
        catch (Exception ex)
        {
            Exception ??= ex;
            //System.Diagnostics.Debugger.Launch();
        }
    }
}