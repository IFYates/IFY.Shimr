
namespace IFY.Shimr.CodeGen.Models.Bindings;

internal interface IBinding
{
    string ClassName { get; }
    IShimDefinition Definition { get; }
    ShimTarget Target { get; }

    void GenerateCode(StringBuilder code);
}