using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

internal class MethodParameterDefinition
{
    public string Name { get; }
    public TypeDef ParameterType { get; }
    public string ParameterTypeFullName => ParameterType.FullName;
    public string? TargetTypeFullName { get; }

    public MethodParameterDefinition(IParameterSymbol parameter)
    {
        Name = parameter.Name;
        ParameterType = parameter.Type is IArrayTypeSymbol array
            ? new(array)
            : new((INamedTypeSymbol)parameter.Type);

        if (parameter.GetAttribute<TypeShimAttribute>()?
            .TryGetAttributeConstructorValue("realType", out var realType) == true)
        {
            TargetTypeFullName = ((INamedTypeSymbol)realType!).FullName();
        }

        // TODO: params, out, ref
        // TODO: auto-shim
    }
}