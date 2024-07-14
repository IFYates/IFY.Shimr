using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal interface IShimMember
{
    ISymbol Symbol { get; }
    string Name { get; }
    string OriginalName { get; }
    void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType);

    /// <summary>
    /// Looks for additional shims required to complete shim.
    /// </summary>
    void ResolveImplicitShims(ShimRegister shimRegister, IShimTarget target);
}
