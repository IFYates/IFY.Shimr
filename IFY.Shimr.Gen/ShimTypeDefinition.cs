using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen;

/// <summary>
/// Represents everything important about a shim type.
/// </summary>
internal class ShimTypeDefinition
{
    public string ShimNamespace { get; }
    public string ShimName { get; }
    public string ShimSafeName { get; }
    public string ShimFullName => $"{ShimNamespace}.{ShimName}";

    public string TargetNamespace { get; }
    public string TargetName { get; }
    public string TargetSafeName { get; }
    public string TargetFullName => $"{TargetNamespace}.{TargetName}";

    public string ShimrName { get; set; } = string.Empty;

    public List<ShimMemberDefinition> Members { get; } = new();

    public ShimTypeDefinition(INamedTypeSymbol interfaceDef, AttributeData attr)
    {
        // Parse interface for details
        ShimNamespace = interfaceDef.FullNamespace();
        ShimName = interfaceDef.GetName();
        ShimSafeName = ShimName.Replace('+', '_').Replace('.', '_').Replace("`", "");

        var targetType = (INamedTypeSymbol)attr.ConstructorArguments[0].Value!;
        TargetNamespace = targetType.FullNamespace();
        TargetName = targetType.GetName();
        TargetSafeName = TargetName.Replace('+', '_').Replace('.', '_').Replace("`", "");

        // Parse interface members
        foreach (var member in interfaceDef.GetMembers())
        {
            //if (member.DeclaredAccessibility == Accessibility.Public)
            //{
            //    continue;
            //}

            switch (member)
            {
                case IPropertySymbol property:
                    Members.Add(new ShimMemberDefinition(property));
                    break;
                case IMethodSymbol method:
                    if (method.ConstructedFrom.AssociatedSymbol?.Kind != SymbolKind.Property)
                    {
                        Members.Add(new ShimMemberDefinition(method));
                    }
                    break;
            }
            // TODO
        }

        // TODO: properties
        // TODO: methods
        // TODO: constructors
        // TODO: auto-shim returns
        // TODO: auto-shim parameters
        // TODO: aliases
        // TODO: other changes
    }
}

internal class ShimMemberDefinition
{
    public enum MemberKind
    {
        Property,
        Method
    }

    public MemberKind Kind { get; }
    public string Name { get; }
    public string? TargetName { get; private set; }
    public string? ReturnTypeFullName { get; }

    public bool CanRead { get; }
    public bool CanWrite { get; }

    public Dictionary<string, string> Parameters { get; } = new();

    public ShimMemberDefinition(IPropertySymbol property)
    {
        // Basics
        Kind = MemberKind.Property;
        Name = property.Name;
        var returnType = property.GetMethod?.ReturnType ?? property.SetMethod!.Parameters[0].Type;
        ReturnTypeFullName = returnType.TryFullName()!;
        if (ReturnTypeFullName == "System.Void")
        {
            ReturnTypeFullName = null;
        }
        CanRead = property.GetMethod != null;
        CanWrite = property.SetMethod != null;

        // Attributes
        parseAttributes(property);
    }

    public ShimMemberDefinition(IMethodSymbol method)
    {
        // Basics
        Kind = MemberKind.Method;
        Name = method.Name;
        ReturnTypeFullName = method.ReturnType.TryFullName()!;
        if (ReturnTypeFullName == "System.Void")
        {
            ReturnTypeFullName = null;
        }

        // Parameters
        foreach (var parameter in method.Parameters)
        {
            parseParameter(parameter);
        }

        // Attributes
        parseAttributes(method);
    }

    private void parseAttributes(ISymbol symbol)
    {
        // ShimAttribute
        var shimAttr = symbol.GetAttributes()
            .SingleOrDefault(a => a.AttributeClass.FullName() == typeof(ShimAttribute).FullName);
        if (shimAttr != null)
        {
            // TODO: support definitionType arg
            if (shimAttr.TryGetAttributeConstructorValue("name", out var targetName))
            {
                TargetName = targetName?.ToString();
            }
        }
    }

    private void parseParameter(IParameterSymbol parameter)
    {
        // TODO: defaults, params, out, ref
        Parameters.Add(parameter.Name, parameter.Type.TryFullName());
    }
}

internal static class SymbolExtensions
{
    public static bool TryGetAttributeConstructorValue(this AttributeData attr, string constructorArgName, out object? value)
    {
        var argIdx = attr.AttributeConstructor!.Parameters
            .Select((a, i) => a.Name == constructorArgName ? i : (int?)null)
            .FirstOrDefault(i => i.HasValue);
        if (argIdx.HasValue)
        {
            value = attr.ConstructorArguments[argIdx.Value].Value;
            return true;
        }
        value = null;
        return false;
    }
}