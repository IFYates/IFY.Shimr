using IFY.Shimr.Gen.SyntaxParsing;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.Gen.Model;

internal abstract class ShimMember
{
    public ISymbol Symbol { get; protected set; } = null!;

    // The full name of the interface that defines this member, if explicit reference needed
    public string? ExplicitInterfaceType { get; set; }

    // The full name of the target type that provides the implementation
    public string? TargetType { get; set; }
    // The full name of the return type of the target member, if different to the shim
    public string? TargetReturnType { get; set; }
    // The name of the target member, if different to the shim member
    public string? TargetName { get; set; }

    public bool IsStatic { get; set; }

    public string ReturnType { get; set; } = null!;
    public string Name { get; set; } = null!;
}

internal class ShimMethodMember : ShimMember
{
    // TODO: is constructor
    // TODO: gen args
    // TODO: params

    public override string ToString()
    {
        // TODO
        return null!;
    }
}

internal class ShimPropertyMember : ShimMember
{
    new public IPropertySymbol Symbol => (IPropertySymbol)base.Symbol;

    public ShimPropertyMember(IPropertySymbol property)
    {
        base.Symbol = property;

        if (property.TryGetReturnType(out var retType))
        {
            ReturnType = retType.FullName;
        }
        Name = property.Name;

        CanRead = property.GetMethod != null;
        CanWrite = property.SetMethod != null;
    }

    // The full name of the property type
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }

    // If the target of this shim is a set of methods (get_, set_)
    public bool UseMethods { get; set; }

    public override string ToString()
    {
        string res = "";
        if (ExplicitInterfaceType == null)
        {
            res += $"public {ReturnType} {Name}";
        }
        else
        {
            res += $"{ReturnType} {ExplicitInterfaceType}.{Name}";
        }

        var objName = "_obj";
        string getValue()
        {
            return $"{objName}.{TargetName ?? Name}"
                + (UseMethods ? "()" : "");
        }

        if (CanRead && !CanWrite)
        {
            res += $"\n\t=> {getValue()};";
        }
        else
        {
            res += "\n{";
            if (CanRead)
            {
                res += $"\n\tget => {getValue()};";
            }
            if (CanWrite)
            {
                var value = "value";

                res += $"\n\tset => {objName}.{TargetName ?? Name}";
                res += UseMethods ? $"({value});" : $" = {value};";
            }
            res += "\n}";
        }

        // etc.
        return res;
    }
}