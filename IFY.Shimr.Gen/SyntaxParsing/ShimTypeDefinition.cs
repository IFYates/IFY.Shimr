using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

/// <summary>
/// Represents everything important about a shim type.
/// </summary>
internal class ShimTypeDefinition
{
    public TypeDef ShimType { get; }
    public string ShimFullName => ShimType.FullName;
    public string ShimSafeName { get; }

    public bool IsStatic { get; }
    public TypeDef TargetType { get; }
    public string TargetNamespace => TargetType.Namespace;
    public string TargetFullName => TargetType.FullName;
    public string TargetSafeName { get; }

    public List<ShimMemberDefinition> Members { get; } = new();

    /// <summary>
    /// Any additional shims found during parsing that will need to be generated.
    /// </summary>
    public List<(TypeDef ShimType, TypeDef TargetType)> AdditionalShims { get; } = new();

    public ShimTypeDefinition(TypeDef interfaceDef, TypeDef targetType, bool isStatic)
    {
        ShimType = interfaceDef;
        ShimSafeName = ShimType.Name.MakeSafeName(); // TODO: could add random suffix

        TargetType = targetType;
        TargetSafeName = TargetType.Name.MakeSafeName(); // TODO: could add random suffix

        // Tidy up generics
        if (!isStatic && TargetType.IsGeneric)
        {
            if (!ShimType.IsGeneric)
            {
                // TODO: fail
            }
            else if (TargetType.GenericArgs.Length != ShimType.GenericArgs.Length)
            {
                // TODO: fail
            }
            else
            {
                // TODO: apply fixed types
                // TODO: apply template names
                TargetType.GenericArgs = ShimType.GenericArgs;
            }
        }
        else if (ShimType.IsGeneric)
        {
            // Allowed; ignored for inheritance
        }

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
                IPropertySymbol property => new ShimMemberDefinition(property, targetType),
                IMethodSymbol method when method.ConstructedFrom.AssociatedSymbol == null
                    => new ShimMemberDefinition(method, targetType),
                IEventSymbol ev => new ShimMemberDefinition(ev),
                _ => null
            };
            if (def == null)
            {
                continue;
            }

            Members.Add(def);

            // Check for return auto-shim
            if (def.ReturnType?.Kind == TypeKind.Interface)
            {
                if (def.IsConstructor)
                {
                    def.StaticType ??= targetType;
                    def.TargetReturnType = targetType;
                    def.IsReturnShim = true;
                    if (!targetType.AllInterfaces.Any(i => i.FullName() == def.ReturnType.FullName))
                    {
                        AdditionalShims.Add((def.ReturnType, targetType));
                    }
                    continue;
                }

                if (def.TargetMember?.TryGetReturnType(out var targetReturnType) == true
                    && !targetReturnType.AllInterfaces.Any(i => i.FullName() == def.ReturnType.FullName)
                    && targetReturnType.FullName != def.ReturnType.FullName)
                {
                    AdditionalShims.Add((def.ReturnType, targetReturnType));
                    def.TargetReturnType = targetReturnType;
                    def.IsReturnShim = true;
                }
            }
        }

        IsStatic = isStatic || Members.Any(m => m.IsStatic);
    }
}