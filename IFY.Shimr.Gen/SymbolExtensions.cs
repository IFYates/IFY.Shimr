using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen;

internal static class SymbolExtensions
{
    public static bool TryGetAttributeConstructorValue(this AttributeData attr, string constructorArgName, out object? value)
    {
        var argIdx = attr.AttributeConstructor!.Parameters
            .Select((a, i) => a.Name == constructorArgName ? i : (int?)null)
            .FirstOrDefault(i => i.HasValue);
        if (argIdx.HasValue)
        {
            value = attr.ConstructorArguments[argIdx.Value].Value;
            return true;
        }
        value = null;
        return false;
    }

    public static bool TryGetReturnType(this ISymbol symbol, [NotNullWhen(true)] out INamedTypeSymbol? returnType)
    {
        var type = symbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.GetMethod?.ReturnType
                ?? property.SetMethod!.Parameters[0].Type,
            IMethodSymbol method => !method.ReturnsVoid
                ? method.ReturnType
                : null,
            _ => null
        };

        returnType = type switch
        {
            IArrayTypeSymbol arr => null, // TODO
            INamedTypeSymbol namedType => namedType,
            _ => null,
        };

        if (returnType?.FullName() == typeof(void).FullName)
        {
            returnType = null;
        }
        return returnType != null;
    }
}