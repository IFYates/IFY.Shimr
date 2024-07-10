using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimMethod(IMethodSymbol symbol) : BaseReturnableShimMember<IMethodSymbol>
{
    public string? Documentation { get; } = symbol.GetDocumentationCommentXml();
    public override string Name { get; } = symbol.Name;
    public override ITypeSymbol ReturnType { get; } = symbol.ReturnType;
    public override string ReturnTypeName { get; } = symbol.ReturnType.ToDisplayString();
    public int ParameterCount { get; } = symbol.Parameters.Length;
    public string Arguments { get; } = string.Join(", ", symbol.Parameters.Select(p => p.Name));
    // TODO: out, ref, default
    // TODO: nullability?
    // TODO: attributes?

    protected override IEnumerable<IMethodSymbol> UnderlyingMemberMatch(IEnumerable<IMethodSymbol> underlyingMembers)
        => underlyingMembers.Where(symbol.AllParameterTypesMatch);

    public override void GenerateCode(StringBuilder code, INamedTypeSymbol underlyingType, IMethodSymbol? underlyingMethod)
    {
        if (Documentation?.Length > 0)
        {
            code.AppendLine(Documentation);
        }

        code.Append($"            public {ReturnTypeName} {Name}({symbol.GetParametersSignature()})");

        if (underlyingMethod == null)
        {
            //System.Diagnostics.Debugger.Launch();
            //var members = underlyingType.GetMembers("ToString").OfType<IMethodSymbol>();
            //var x = UnderlyingMemberMatch(members);

            // TODO: optional, as per 'IgnoreMissingMembers'
            code.AppendLine(" => throw new NotImplementedException(/* TODO: explanation */);");
            return;
        }

        code.Append($" => _inst.{Name}({Arguments})").Append(GetShimCode(underlyingMethod.ReturnType)).AppendLine(";");
    }
}
