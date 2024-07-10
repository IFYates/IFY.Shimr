using Microsoft.CodeAnalysis;
using System.Text;

namespace IFY.Shimr.CodeGen.Models;

internal interface IShimMember
{
    string Name { get; }
    void GenerateCode(StringBuilder code, INamedTypeSymbol underlyingType);
}
