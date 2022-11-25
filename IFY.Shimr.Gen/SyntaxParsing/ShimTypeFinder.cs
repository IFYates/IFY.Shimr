using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Not sure how to test without unnecessary restructure
internal class ShimTypeFinder : ISyntaxContextReceiver
{
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
                var shimrAttrs = interfaceDef.GetAttributes<ShimOfAttribute>().ToArray();
                foreach (var attr in shimrAttrs)
                {
                    if (attr.TryGetAttributeConstructorValue("targetType", out var targetType))
                    {
                        ShimTypes.Add(new ShimTypeDefinition(new(interfaceDef), new((INamedTypeSymbol)targetType!), false));
                    }
                }

                // Add each shimmed type to list
                var staticAttr = interfaceDef.GetAttribute<StaticShimAttribute>();
                if (staticAttr?.TryGetAttributeConstructorValue("targetType", out var staticTargetType) == true)
                {
                    ShimTypes.Add(new ShimTypeDefinition(new(interfaceDef), new((INamedTypeSymbol)staticTargetType!), true));
                }
            }
        }
        catch (Exception ex)
        {
            _ = ex.ToString();
            Debugger.Launch();
            throw;
        }
    }
}