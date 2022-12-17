using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

internal static class SymbolExtensions
{
    public static ISymbol[] GetAllMembers(this ITypeSymbol type)
    {
        var members = new List<ISymbol>(type.GetMembers());
        members.AddRange(type.AllInterfaces.SelectMany(i => i.GetMembers()));
        while (type.BaseType.FullName() != "System.Object")
        {
            type = type.BaseType!;
            members.AddRange(type.GetMembers());
        }
        return members.Distinct().ToArray();
    }

    public static string MakeSafeName(this string str)
    {
        return str.Replace('+', '_').Replace('.', '_').Replace("`", "")
            .Replace("<", "").Replace(",", "").Replace(">", "").Replace(" ", "").TrimEnd('?');
    }

    public static bool TryGetAttributeConstructorValue(this AttributeData attr, string constructorArgName, out object? value)
    {
        var argIdx = attr.AttributeConstructor?.Parameters
            .Select((a, i) => a.Name == constructorArgName ? i : (int?)null)
            .FirstOrDefault(i => i.HasValue);
        if (argIdx != null)
        {
            value = attr.ConstructorArguments[argIdx.Value].Value;
            return true;
        }
        value = null;
        return false;
    }

    public static bool TryGetReturnType(this ISymbol symbol, [NotNullWhen(true)] out TypeDef? returnType)
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
            IArrayTypeSymbol arr => new(arr),
            INamedTypeSymbol namedType => new(namedType),
            ITypeParameterSymbol typepar => new(typepar),
            _ => null,
        };

        if (returnType?.FullName == typeof(void).FullName)
        {
            returnType = null;
        }
        return returnType != null;
    }
}