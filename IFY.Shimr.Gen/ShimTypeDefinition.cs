using Microsoft.CodeAnalysis;
using System.Diagnostics;
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
    public string? ReturnTypeFullName { get; }

    public bool CanRead { get; }
    public bool CanWrite { get; }

    public Dictionary<string, string> Parameters { get; } = new();

    public ShimMemberDefinition(IPropertySymbol property)
    {
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
    }

    public ShimMemberDefinition(IMethodSymbol method)
    {
        Kind = MemberKind.Method;
        Name = method.Name;
        ReturnTypeFullName = method.ReturnType.TryFullName()!;
        if (ReturnTypeFullName == "System.Void")
        {
            ReturnTypeFullName = null;
        }

        foreach (var param in method.Parameters)
        {
            // TODO: defaults, params, out, ref
            Parameters.Add(param.Name, param.Type.TryFullName());
        }
    }
}
