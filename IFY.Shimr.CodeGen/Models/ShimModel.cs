using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Models an interface-underlying shim combination.
/// </summary>
internal class ShimModel
{
    public ShimterfaceModel Shimterface { get; }
    public string InterfaceFullName => Shimterface.InterfaceFullName;
    public INamedTypeSymbol UnderlyingType { get; }
    public string UnderlyingFullName { get; }

    public string Key { get; }
    public string Name { get; }

    public ShimModel(ShimterfaceModel shimterface, INamedTypeSymbol underlyingType)
    {
        Shimterface = shimterface;
        UnderlyingType = underlyingType;
        UnderlyingFullName = UnderlyingType.ToDisplayString();
        Key = shimterface.InterfaceType.Name + "_" + underlyingType.Name;
        Name = Key + "_" + ShimterfaceModel.R.Next();
    }
}
