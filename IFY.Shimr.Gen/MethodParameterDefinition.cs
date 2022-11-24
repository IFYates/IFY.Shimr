using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen;

internal class MethodParameterDefinition
{
    public string Name { get; }
    public string ParameterTypeFullName { get; }
    public string? TargetTypeFullName { get; }

    public MethodParameterDefinition(IParameterSymbol parameter)
    {
        Name = parameter.Name;
        ParameterTypeFullName = parameter.Type.TryFullName();

        if (parameter.GetAttribute<TypeShimAttribute>()?
            .TryGetAttributeConstructorValue("realType", out var realType) == true)
        {
            TargetTypeFullName = ((INamedTypeSymbol)realType!).FullName();
        }
        
        // TODO: params, out, ref
        // TODO: auto-shim
    }
}