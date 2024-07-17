using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// A model of an instance or static shim definition.
/// </summary>
internal interface IShimDefinition
{
    INamedTypeSymbol Symbol { get; }
    string FullTypeName { get; }
    string Name { get; }

    void WriteShimClass(ICodeWriter writer, IEnumerable<IBinding> bindings);

    void Resolve(IList<IBinding> allBindings, CodeErrorReporter errors, ShimResolver shimResolver);
}
