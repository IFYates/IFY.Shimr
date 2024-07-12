using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal class ShimMemberMethod(BaseShimType baseShimType, IMethodSymbol symbol)
    : BaseReturnableShimMember<IMethodSymbol>(baseShimType, symbol)
{
    public override ITypeSymbol ReturnType { get; } = symbol.ReturnType;
    public override string ReturnTypeName { get; } = symbol.ReturnType.ToDisplayString();

    public ShimMemberMethodParameter[] Parameters { get; }
        = symbol.Parameters.Select(p => new ShimMemberMethodParameter(p)).ToArray();

    protected override IEnumerable<IMethodSymbol> UnderlyingMemberMatch(IEnumerable<IMethodSymbol> underlyingMembers)
        => underlyingMembers.Where(Symbol.AllParameterTypesMatch);

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType, IMethodSymbol? underlyingMethod)
    {
        code.Append($"            public {ReturnTypeName} {Name}(")
            .Append(string.Join(", ", Parameters.Select(p => p.ToString())))
            .Append(")");

        if (underlyingMethod == null)
        {
            errors.NoMemberError(Symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()!, underlyingType.ToDisplayString(), Name /* TODO: full signature */);

            // TODO: optional, as per 'IgnoreMissingMembers'
            code.AppendLine(" => throw new NotImplementedException(/* TODO: explanation */);");
            return;
        }

        var callee = underlyingMethod.IsStatic ? underlyingType.ToDisplayString() : "_inst";

        code.Append($" => {callee}.{Name}(")
            .Append(string.Join(", ", Parameters.Select(p => p.GetTargetArgumentCode())))
            .Append($")")
            .Append(GetShimCode(underlyingMethod.ReturnType))
            .AppendLine(";");
    }

    public override ITypeSymbol GetUnderlyingMemberReturn(ITypeSymbol underlyingType)
        => GetUnderlyingMember(underlyingType)?.ReturnType ?? ReturnType;

    protected override void DoResolveImplicitShims(ShimRegister shimRegister, IShimTarget target)
    {
        // Argument overrides
        foreach (var param in Parameters)
        {
            param.ResolveImplicitShims(shimRegister);
        }
    }
}
