using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// A model of a shimmed type.
/// </summary>
internal class ShimTarget(ITypeSymbol symbol, IShimDefinition shim)
{
    public ITypeSymbol Symbol { get; } = symbol;
    public string FullTypeName { get; } = symbol.ToFullName();
    public string Name { get; } = symbol.Name;
    public IShimDefinition Shim { get; } = shim;
    public bool IsValueType { get; } = symbol.IsValueType;

    /// <summary>
    /// Find the <see cref="TargetMember"/>s in this <see cref="ShimTarget"/> that could be bound to the given <see cref="ShimMember"/>.
    /// </summary>
    public TargetMember[] GetMatchingMembers(ShimMember shim)
    {
        var members = Symbol.GetAllMembers()
            .Where(shim.IsMatch)
            .Select(m => TargetMember.Parse(m, this))
            .OfType<TargetMember>().ToArray();
        if (shim.ReturnType is null || members.Length < 2)
        {
            // None or only choice
            return members;
        }

        // Perfect match
        var matchMembers = members.Where(m => shim.ReturnType!.IsMatch(m.ReturnType)).ToArray();
        if (matchMembers.Any())
        {
            return matchMembers;
        }

        // Shimmable
        matchMembers = members.Where(m => m.IsShimmableReturnType(shim)).ToArray();
        if (matchMembers.Any())
        {
            return matchMembers;
        }

        // Fallback
        return members;
    }
}
