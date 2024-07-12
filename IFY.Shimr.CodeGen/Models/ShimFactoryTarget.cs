using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimFactoryTarget(BaseShimType shimType, ITypeSymbol underlyingType, ISymbol? singleMember, bool isConstructor)
    : ShimClassTarget(shimType, underlyingType)
{
    public override string Name { get; } = $"{shimType.InterfaceType.Name}_{underlyingType.Name}_Factory";

    public ISymbol? SingleMember { get; } = singleMember;
    public bool IsConstructor { get; } = isConstructor;

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        // Shim'd members
        var members = ShimType.ResolveShimMembers();
        if (SingleMember != null)
        {
            members = members.Where(m => m.Symbol.Equals(SingleMember, SymbolEqualityComparer.Default)).ToArray();
        }

        // Constructor
        if (IsConstructor)
        {
            // TODO: Find target constructor, or error

            var member = (ShimMemberMethod)members.Single();
            code.Append($"            public {member.ReturnTypeName} {member.Name}(")
                .Append(string.Join(", ", member.Parameters.Select(p => p.ToString())))
                .Append(")");

            code.Append($" => new {UnderlyingFullName}(")
                .Append(string.Join(", ", member.Parameters.Select(p => p.GetTargetArgumentCode())))
                .Append($")")
                .Append(member.GetShimCode(UnderlyingType))
                .AppendLine(";");
            return;
        }

        foreach (var member in members)
        {
            member.GenerateCode(code, errors, UnderlyingType);
        }
    }

    public override void DoResolveImplicitShims(ShimRegister shimRegister)
    {
        if (IsConstructor)
        {
            shimRegister.GetOrCreate(((IMethodSymbol)SingleMember!).ReturnType)
                .AddShim(UnderlyingType);
        }
    }
}