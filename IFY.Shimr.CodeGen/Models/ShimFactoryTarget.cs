using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimFactoryTarget(BaseShimType shimType, ITypeSymbol underlyingType, ISymbol? member, bool isConstructor)
    : ShimClassTarget(shimType, underlyingType)
{
    public override string Name { get; } = $"{shimType.InterfaceType.Name}_{underlyingType.Name}_Factory{(member != null ? $"_{member.ToUniqueName()}" : null)}"; // TODO: better unique name

    public ISymbol? SingleMember { get; } = member;
    public bool IsConstructor { get; } = isConstructor;

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        throw new NotSupportedException();
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