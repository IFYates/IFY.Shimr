using Microsoft.CodeAnalysis;
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

    public string ShimrName { get; }

    public List<ShimMemberDefinition> Members { get; } = new();

    /// <summary>
    /// Any additional shims found during parsing that will need to be generated.
    /// </summary>
    public List<(INamedTypeSymbol ShimType, INamedTypeSymbol TargetType)> AdditionalShims { get; } = new();

    private static string MakeSafeName(string str)
    {
        return str.Replace('+', '_').Replace('.', '_').Replace("`", "").TrimEnd('?');
    }

    public ShimTypeDefinition(INamedTypeSymbol interfaceDef, INamedTypeSymbol targetType)
    {
        // Parse interface for details
        ShimNamespace = interfaceDef.FullNamespace().TrimEnd('?');
        ShimName = interfaceDef.GetName().TrimEnd('?');
        ShimSafeName = MakeSafeName(ShimName);

        TargetNamespace = targetType.FullNamespace().TrimEnd('?');
        TargetName = targetType.GetName().TrimEnd('?');
        TargetSafeName = MakeSafeName(TargetName);

        ShimrName = $"{ShimSafeName}__{TargetSafeName}";

        // Parse interface members
        foreach (var member in interfaceDef.GetMembers())
        {
            if (member.DeclaredAccessibility != Accessibility.Public
                || !member.IsAbstract)
            {
                continue;
            }

            // Shape
            var def = member switch
            {
                IPropertySymbol property => new ShimMemberDefinition(property),
                IMethodSymbol method when method.ConstructedFrom.AssociatedSymbol?.Kind != SymbolKind.Property
                    => new ShimMemberDefinition(method),
                _ => null
            };
            if (def == null)
            {
                continue;
            }

            Members.Add(def);

            // Check for return auto-shim
            if (def.ReturnType?.TypeKind == TypeKind.Interface)
            {
                var defReturnTypeName = def.ReturnType.TryFullName();
                var targetMember = targetType.GetMembers()
                    .Where(m => m.Kind == def.Kind && m.Name == (def.TargetName ?? def.Name))
                    .FirstOrDefault();
                if (targetMember.TryGetReturnType(out var targetReturnType)
                    && !targetReturnType.AllInterfaces.Any(i => i.FullName() == defReturnTypeName))
                {
                    AdditionalShims.Add((def.ReturnType, targetReturnType));
                    def.TargetReturnType = targetReturnType;
                }
            }
        }

        // TODO: properties
        // TODO: methods
        // TODO: constructors
        // TODO: auto-shim parameters
        // TODO: aliases
        // TODO: other changes
    }
}