using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Not sure how to test without unnecessary restructure
internal class ShimTypeFinder : ISyntaxContextReceiver
{
    public List<ShimTypeDefinition> ShimTypes { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        // Only interested in certain interfaces
        if (context.Node is InterfaceDeclarationSyntax)
        {
            var interfaceDef = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!;

            // Must be decorated with ShimrAttribute
            var shimrAttrs = interfaceDef.GetAttributes()
                .Where(a => a.AttributeClass?.FullName() == typeof(ShimrAttribute).FullName)
                .ToArray();
            if (!shimrAttrs.Any())
            {
                return;
            }

            // Add to list for generation
            foreach (var attr in shimrAttrs)
            {
                ShimTypes.Add(new ShimTypeDefinition(interfaceDef, attr));
            }
        }
    }
}