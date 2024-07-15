namespace IFY.Shimr.CodeGen.Models.Bindings;

/// <summary>
/// An empty binding.
/// </summary>
internal class NullBinding(IShimDefinition shim, ShimTarget target) : IBinding
{
    public string ClassName { get; } = $"Shim_{shim.Name}_{target.Name}";
    public IShimDefinition Definition => shim;
    public ShimTarget Target => target;

    public virtual void GenerateCode(StringBuilder code)
    {
    }
}
