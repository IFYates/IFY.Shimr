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
        returnType = null;
        switch (symbol)
        {
            case IFieldSymbol field:
                returnType = (INamedTypeSymbol?)field.Type;
                break;
            case IPropertySymbol property:
                returnType = (INamedTypeSymbol?)property.GetMethod?.ReturnType
                    ?? (INamedTypeSymbol)property.SetMethod!.Parameters[0].Type;
                break;
            case IMethodSymbol method:
                returnType = !method.ReturnsVoid
                    ? (INamedTypeSymbol)method.ReturnType
                    : null;
                break;
        }

        if (returnType?.FullName() == typeof(void).FullName)
        {
            returnType = null;
        }
        return returnType != null;
    }
}