using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal interface IShimTarget
{
    string Name { get; }

    BaseShimType ShimType { get; }
    string InterfaceFullName { get; }
    ITypeSymbol UnderlyingType { get; }
    string UnderlyingFullName { get; }

    /// <summary>
    /// Looks for additional shims required to complete shim.
    /// </summary>
    void ResolveImplicitShims(ShimRegister shimRegister);
}