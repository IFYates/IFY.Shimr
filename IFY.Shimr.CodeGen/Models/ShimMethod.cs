using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimMethod(IMethodSymbol symbol) : BaseReturnableShimMember<IMethodSymbol>
{
    public override string Name { get; } = symbol.Name;

    public override ITypeSymbol ReturnType { get; } = symbol.ReturnType;
    public override string ReturnTypeName { get; } = symbol.ReturnType.ToDisplayString();

    public ShimMethodParameter[] Parameters { get; }
        = symbol.Parameters.Select(p => new ShimMethodParameter(p)).ToArray();

    protected override IEnumerable<IMethodSymbol> UnderlyingMemberMatch(IEnumerable<IMethodSymbol> underlyingMembers)
        => underlyingMembers.Where(symbol.AllParameterTypesMatch);

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType, IMethodSymbol? underlyingMethod)
    {
        code.Append($"            public {ReturnTypeName} {Name}(")
            .Append(string.Join(", ", Parameters.Select(p => p.ToString())))
            .Append(")");

        if (underlyingMethod == null)
        {
            //System.Diagnostics.Debugger.Launch();
            //var members = underlyingType.GetMembers("ToString").OfType<IMethodSymbol>();
            //var x = UnderlyingMemberMatch(members);

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
}
