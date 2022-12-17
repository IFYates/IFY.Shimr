using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

internal class MethodParameterDefinition
{
    public IParameterSymbol Symbol { get; }
    public string Name { get; }
    public TypeDef ParameterType { get; }
    public string ParameterTypeFullName => ParameterType.FullName;
    public string? TargetTypeFullName { get; }

    public MethodParameterDefinition(IParameterSymbol symbol)
    {
        Symbol = symbol;
        Name = symbol.Name;
        ParameterType = symbol.Type is IArrayTypeSymbol array
            ? new(array)
            : symbol.Type is ITypeParameterSymbol typepar
            ? new(typepar)
            : new((INamedTypeSymbol)symbol.Type);

        if (symbol.GetAttribute<TypeShimAttribute>()?
            .TryGetAttributeConstructorValue("realType", out var realType) == true)
        {
            TargetTypeFullName = ((INamedTypeSymbol)realType!).FullName();
        }

        // TODO: params, out, ref
        // TODO: auto-shim
    }
}