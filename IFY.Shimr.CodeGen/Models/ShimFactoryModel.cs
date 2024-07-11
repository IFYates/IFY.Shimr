using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimFactoryModel(ShimterfaceModel shimterface, ITypeSymbol underlyingType)
    : ShimModel(shimterface, underlyingType, "_Factory")
{
}