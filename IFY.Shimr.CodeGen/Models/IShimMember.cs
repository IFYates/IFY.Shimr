using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal interface IShimMember
{
    string Name { get; }
    void GenerateCode(StringBuilder code, CodeErrorReporter errors, ITypeSymbol underlyingType);
}
