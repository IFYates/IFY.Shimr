using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Bindings;

internal interface IBinding
{
    string ClassName { get; }
    IShimDefinition Definition { get; }
    ShimTarget Target { get; }

    ITypeSymbol? ReturnOverride { get; set; }
    bool IsEnumerableReturnOverride { get; set; }

    void GenerateCode(ICodeWriter writer);
}