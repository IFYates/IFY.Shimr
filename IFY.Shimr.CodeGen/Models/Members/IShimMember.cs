using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal interface IShimMember
{
    ISymbol Symbol { get; }
    string Name { get; }
    void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType);
}
