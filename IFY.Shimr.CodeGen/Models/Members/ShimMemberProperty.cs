using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal class ShimMemberProperty(BaseShimType baseShimType, IPropertySymbol symbol)
    : BaseReturnableShimMember<IPropertySymbol>(baseShimType, symbol)
{
    public override ITypeSymbol ReturnType { get; } = symbol.Type;
    public override string ReturnTypeName { get; } = symbol.Type.ToDisplayString();

    public bool IsGet { get; } = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public;
    public bool IsSet { get; } = symbol.SetMethod?.DeclaredAccessibility == Accessibility.Public && symbol.SetMethod?.IsInitOnly == false;
    public bool IsInit { get; } = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public && symbol.SetMethod?.IsInitOnly == true;

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType, IPropertySymbol? underlyingProperty)
    {
        code.Append($"            public {ReturnTypeName} {Name} {{");

        if (underlyingProperty == null)
        {
            errors.NoMemberError(Symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()!, underlyingType.ToDisplayString(), Name /* TODO: full signature */);

            // TODO: optional, as per 'IgnoreMissingMembers'
            if (IsGet)
            {
                code.Append(" get => throw new System.NotImplementedException(/* TODO: explanation */);");
            }
            if (IsSet)
            {
                code.Append(" set => throw new System.NotImplementedException(/* TODO: explanation */);");
            }
            if (IsInit)
            {
                code.Append(" init => throw new System.NotImplementedException(/* TODO: explanation */);");
            }
        }
        else
        {
            var callee = GetMemberCallee(underlyingType, underlyingProperty);
            if (IsGet)
            {
                code.Append($" get => {callee}.{Name}{GetShimCode(underlyingProperty.Type)};");
            }
            if (IsSet)
            {
                code.Append($" set => {callee}.{Name} = value{GetUnshimCode(underlyingProperty.Type)};");
            }
            if (IsInit)
            {
                code.Append($" init => {callee}.{Name} = value{GetUnshimCode(underlyingProperty.Type)};");
            }
        }

        code.AppendLine(" }");
    }

    public override ITypeSymbol? GetMemberReturn(IPropertySymbol? member)
        => member?.Type;
}
