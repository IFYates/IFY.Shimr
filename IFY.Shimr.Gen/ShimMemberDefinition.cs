using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen;

internal class ShimMemberDefinition
{
    public SymbolKind Kind { get; }
    public string Name { get; }
    public string? TargetName { get; private set; }
    public INamedTypeSymbol? ReturnType { get; }
    public INamedTypeSymbol? TargetReturnType { get; set; }
    public bool IsReturnShim => TargetReturnType != null && TargetReturnType != ReturnType;

    public bool CanRead { get; }
    public bool CanWrite { get; }

    public string? StaticTypeFullName { get; private set; }
    public bool IsConstructor { get; private set; }

    public Dictionary<string, MethodParameterDefinition> Parameters { get; } = new();

    public ShimMemberDefinition(IPropertySymbol property)
    {
        // Basics
        Kind = SymbolKind.Property;
        Name = property.Name;
        CanRead = property.GetMethod != null;
        CanWrite = property.SetMethod != null;
        if (property.TryGetReturnType(out var returnType))
        {
            ReturnType = returnType;
        }

        // Attributes
        parseAttributes(property);
    }

    public ShimMemberDefinition(IMethodSymbol method)
    {
        // Basics
        Kind = SymbolKind.Method;
        Name = method.Name;
        if (method.TryGetReturnType(out var returnType))
        {
            ReturnType = returnType;
        }

        // Parameters
        foreach (var parameter in method.Parameters)
        {
            Parameters.Add(parameter.Name, new MethodParameterDefinition(parameter));
        }

        // Attributes
        parseAttributes(method);
    }

    private void parseAttributes(ISymbol symbol)
    {
        // ShimAttribute
        var shimAttr = symbol.GetAttribute<ShimAttribute>();
        if (shimAttr != null)
        {
            // TODO: support definitionType arg
            if (shimAttr.TryGetAttributeConstructorValue("name", out var targetName))
            {
                TargetName = targetName?.ToString();
            }
        }

        // StaticShimAttribute
        var staticAttr = symbol.GetAttribute<StaticShimAttribute>();
        if (staticAttr?.TryGetAttributeConstructorValue("targetType", out var staticTargetType) == true)
        {
            StaticTypeFullName = ((INamedTypeSymbol)staticTargetType!).FullName();
        }
    }
}
