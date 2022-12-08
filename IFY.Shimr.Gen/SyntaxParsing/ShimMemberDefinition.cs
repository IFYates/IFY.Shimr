using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

internal class ShimMemberDefinition
{
    public string ParentTypeFullName { get; private set; }

    public SymbolKind Kind { get; }
    public string Name { get; }
    public string SignatureName { get; }
    public string? TargetName { get; private set; }
    public TypeDef? ReturnType { get; private set; }
    public TypeDef? TargetReturnType { get; set; }
    public bool IsReturnShim { get; set; }

    public bool CanRead { get; }
    public bool CanWrite { get; }

    public TypeDef? StaticType { get; set; }
    public bool IsStatic { get; private set; }
    public bool IsConstructor { get; private set; }

    public string[]? GenericContraints { get; }

    public Dictionary<string, MethodParameterDefinition> Parameters { get; } = new();

    public ShimMemberDefinition(IPropertySymbol property)
    {
        // Basics
        ParentTypeFullName = property.ContainingType.FullName();
        Kind = SymbolKind.Property;
        Name = property.Name;
        SignatureName = Name;
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
        ParentTypeFullName = method.ContainingType.FullName();
        Kind = SymbolKind.Method;
        Name = method.Name;
        if (method.TryGetReturnType(out var returnType))
        {
            ReturnType = returnType;
        }

        // Type Parameters
        if (method.TypeParameters.Any())
        {
            var constraints = new List<string>();
            for (var i = 0; i < method.TypeParameters.Length; ++i)
            {
                Name += (i == 0 ? "<" : ", ") + method.TypeParameters[i].Name;

                var constraint = method.TypeParameters[i].TypeConstraintString();
                if (constraint?.Length > 0)
                {
                    constraints.Add(constraint);
                }
            }
            Name += ">";
            if (constraints.Any())
            {
                GenericContraints = constraints.ToArray();
            }
        }

        // Parameters
        foreach (var parameter in method.Parameters)
        {
            Parameters.Add(parameter.Name, new MethodParameterDefinition(parameter));
        }

        SignatureName = Name + (method.TypeArguments.Any() ? "`" + method.TypeArguments.Length : null)
            + "(" + string.Join(",", method.Parameters.Select(p => p.Type.Name)) + ")";

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
            StaticType = staticTargetType is INamedTypeSymbol type ? new(type) : null;
            IsStatic = true;
        }

        // ConstructorShimAttribute
        var constrAttr = symbol.GetAttribute<ConstructorShimAttribute>();
        if (constrAttr != null)
        {
            IsConstructor = true;
            IsStatic = true;
            constrAttr.TryGetAttributeConstructorValue("targetType", out var constrTargetType);
            StaticType = constrTargetType is INamedTypeSymbol type ? new(type) : null;
            TargetReturnType = constrTargetType is INamedTypeSymbol type2 ? new(type2) : null;
            IsReturnShim = true;
        }
    }
}
