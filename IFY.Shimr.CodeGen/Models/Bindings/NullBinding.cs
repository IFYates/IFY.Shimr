using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Bindings;

/// <summary>
/// An empty binding.
/// </summary>
internal class NullBinding(IShimDefinition shim, ShimTarget target)
    : IBinding
{
    public string ClassName { get; } = $"Shim__{shim.Name}__{target.Name}";
    public IShimDefinition Definition => shim;
    public ShimTarget Target => target;

    public ITypeSymbol? ReturnOverride { get; set; }
    public bool IsEnumerableReturnOverride { get; set; }

    public virtual void GenerateCode(ICodeWriter writer)
    {
    }
}
