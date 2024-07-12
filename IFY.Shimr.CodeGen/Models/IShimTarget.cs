using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal interface IShimTarget
{
    string Name { get; }

    BaseShimType ShimType { get; }
    string InterfaceFullName { get; }
    ITypeSymbol UnderlyingType { get; }
    string UnderlyingFullName { get; }
    void ResolveImplicitShims(ShimRegister shimRegister, IList<IShimTarget> shims);
}