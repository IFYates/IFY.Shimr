using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimFactoryTarget(BaseShimType shimterface, ITypeSymbol underlyingType, ISymbol? singleMember)
    : ShimClassTarget(shimterface, underlyingType)
{
    public override string Name { get; } = $"{shimterface.InterfaceType.Name}_{underlyingType.Name}_Factory";

    public ISymbol? SingleMember { get; } = singleMember;

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        // Shim'd members
        var members = ShimType.ResolveShimMembers();

        if (SingleMember != null)
        {
            members = members.Where(m => m.Symbol.Equals(SingleMember, SymbolEqualityComparer.Default)).ToArray();
            Diag.WriteOutput("// SM " + members.Length);
        }

        foreach (var member in members)
        {
            member.GenerateCode(code, errors, UnderlyingType);
        }
    }
}