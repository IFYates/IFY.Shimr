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

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType, IMethodSymbol? underlyingMethod)
    {
        code.Append($"            public {ReturnTypeName} {Name}(")
            .Append(string.Join(", ", Parameters.Select(p => p.ToString())))
            .Append(")");

        if (underlyingMethod == null)
        {
            errors.NoMemberError(Symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(), underlyingType.ToDisplayString(), Name /* TODO: full signature */);

            // TODO: optional, as per 'IgnoreMissingMembers'
            code.AppendLine(" => throw new System.NotImplementedException(/* TODO: explanation */);");
            return;
        }

        code.Append($" => {GetMemberCallee(underlyingType, underlyingMethod)}.{OriginalName}(")
            .Append(string.Join(", ", Parameters.Select(p => p.GetTargetArgumentCode())))
            .Append($")")
            .Append(GetShimCode(underlyingMethod.ReturnType))
            .AppendLine(";");
    }

    public override ITypeSymbol? GetMemberReturn(IMethodSymbol? member)
        => member?.ReturnType;

    protected override bool IsUnderlyingMemberMatch(ISymbol member)
        => Symbol.AllParameterTypesMatch((IMethodSymbol)member);

    protected override void DoResolveImplicitShims(ShimRegister shimRegister, IShimTarget target)
    {
        // Argument overrides
        foreach (var param in Parameters)
        {
            param.ResolveImplicitShims(shimRegister);
        }
    }
}
