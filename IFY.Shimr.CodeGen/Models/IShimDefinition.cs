using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// A model of an instance or static shim definition.
/// </summary>
internal interface IShimDefinition
{
    string FullTypeName { get; }
    string Name { get; }
    int TargetCount { get; }

    void WriteShimClass(ICodeWriter writer, IEnumerable<IBinding> bindings);

    void Resolve(IList<IBinding> allBindings, CodeErrorReporter errors, ShimResolver shimResolver);
}
