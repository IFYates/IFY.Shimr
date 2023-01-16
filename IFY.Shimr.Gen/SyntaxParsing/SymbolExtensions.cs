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
        while (type != null && type.BaseType.FullName() != "System.Object")
        {
            type = type?.BaseType!;
            if (type != null)
            {
                members.AddRange(type.GetMembers());
            }
        }
        return members.Distinct().ToArray();
    }

    public static IMethodSymbol[] GetMatchingMethods(this ITypeSymbol type, string name, ITypeSymbol? returnType, IEnumerable<IParameterSymbol> parameters, bool isStatic)
    {
        return type.GetAllMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.Name == name && m.IsStatic == isStatic)
            .Where(m => (returnType == null && m.ReturnsVoid) || m.ReturnType.TryFullName() == returnType?.TryFullName())
            .Where(allParametersMatch)
            .ToArray();
        bool allParametersMatch(IMethodSymbol method)
        {
            if (method.Parameters.Length != parameters.Count())
            {
                return false;
            }
            return parameters
                .Select((p, i) => p.Type.TryFullName() == method.Parameters[i].Type.TryFullName())
                .All(v => v);
        }
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