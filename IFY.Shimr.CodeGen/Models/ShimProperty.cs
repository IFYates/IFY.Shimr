using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimProperty(IPropertySymbol symbol) : BaseReturnableShimMember<IPropertySymbol>
{
    public override string Name { get; } = symbol.Name;
    public override ITypeSymbol ReturnType { get; } = symbol.Type;
    public override string ReturnTypeName { get; } = symbol.Type.ToDisplayString();
    public bool IsGet { get; } = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public;
    public bool IsSet { get; } = symbol.SetMethod?.DeclaredAccessibility == Accessibility.Public && symbol.SetMethod?.IsInitOnly == false;
    public bool IsInit { get; } = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public && symbol.SetMethod?.IsInitOnly == true;

    public override void GenerateCode(StringBuilder code, ITypeSymbol underlyingType, IPropertySymbol? underlyingProperty)
    {
        code.Append($"            public {ReturnTypeName} {Name} {{");

        if (underlyingProperty == null)
        {
            // TODO: optional, as per 'IgnoreMissingMembers'
            if (IsGet)
            {
                code.Append("get => throw new NotImplementedException(/* TODO: explanation */);");
            }
            if (IsSet)
            {
                code.Append($" set => throw new NotImplementedException(/* TODO: explanation */);");
            }
            if (IsInit)
            {
                code.Append($" init => throw new NotImplementedException(/* TODO: explanation */);");
            }
        }
        else
        {
            if (IsGet)
            {
                code.Append($" get => _inst.{Name}").Append(GetShimCode(underlyingProperty.Type)).Append(";");
            }
            if (IsSet)
            {
                code.Append($" set => _inst.{Name} = value").Append(GetUnshimCode(underlyingProperty.Type)).Append(";");
            }
            if (IsInit)
            {
                code.Append($" init => _inst.{Name} = value").Append(GetUnshimCode(underlyingProperty.Type)).Append(";");
            }
        }

        code.AppendLine(" }");
    }

    public override ITypeSymbol GetUnderlyingMemberReturn(ITypeSymbol underlyingType)
        => GetUnderlyingMember(underlyingType)?.Type ?? ReturnType;
}
