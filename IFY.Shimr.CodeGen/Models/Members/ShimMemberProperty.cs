using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

// TODO: Split Property/Field by resolving on detection
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

        ISymbol? underlyingMember = underlyingProperty;
        IFieldSymbol? underlyingField = null;
        if (underlyingProperty == null)
        {
            underlyingField = underlyingType.GetAllMembers().OfType<IFieldSymbol>()
                .Where(m => m.Name == OriginalName)
                .OrderByDescending(m => m.Type.IsMatch(ReturnType))
                .FirstOrDefault();
            underlyingMember = underlyingField;
        }

        if (underlyingMember != null)
        {
            var callee = GetMemberCallee(underlyingType, underlyingMember);
            var underlyingMemberType = underlyingProperty?.Type ?? underlyingField!.Type;
            if (IsGet)
            {
                code.Append($" get => {callee}.{OriginalName}{GetShimCode(underlyingMemberType)};");
            }
            if (IsSet)
            {
                code.Append($" set => {callee}.{OriginalName} = value{GetUnshimCode(underlyingMemberType)};");
            }
            if (IsInit)
            {
                code.Append($" init => {callee}.{OriginalName} = value{GetUnshimCode(underlyingMemberType)};");
            }
        }
        else
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

        code.AppendLine(" }");
    }

    public override ITypeSymbol? GetMemberReturn(IPropertySymbol? member)
        => member?.Type;

    public override IEnumerable<ISymbol> GetUnderlyingMembersByType(ITypeSymbol underlyingType)
        => underlyingType.GetAllMembers().OfType<IFieldSymbol>().Cast<ISymbol>()
        .Concat(underlyingType.GetAllMembers().OfType<IPropertySymbol>());
}
