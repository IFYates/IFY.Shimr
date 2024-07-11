using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal interface IShimMember
{
    string Name { get; }
    void GenerateCode(StringBuilder code, ITypeSymbol underlyingType);
}
