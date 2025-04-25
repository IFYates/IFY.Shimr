using IFY.Shimr.SourceGen.CodeAnalysis;
using IFY.Shimr.SourceGen.Models.Bindings;
using IFY.Shimr.SourceGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen.Models;

/// <summary>
/// A model of a shimmed type.
/// </summary>
internal class ShimTarget(ITypeSymbol symbol)
{
    public ITypeSymbol Symbol { get; } = symbol;
    public string FullTypeName { get; } = symbol.ToFullName();
    public string Name { get; } = $"{symbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Hash()}_{symbol.ToClassName().Replace('.', '_')}";
    public bool IsValueType { get; } = symbol.IsValueType;

    public virtual IBinding GetBinding(ShimMember shimMember, TargetMember targetMember, ShimTarget? target = null)
    {
        return new ShimMemberBinding(shimMember, targetMember, target);
    }

    /// <summary>
    /// Find the <see cref="TargetMember"/>s in this <see cref="ShimTarget"/> that could be bound to the given <see cref="ShimMember"/>.
    /// </summary>
    public virtual TargetMember[] GetMatchingMembers(ShimMember shimMember)
    {
        return getMatchingMembers(shimMember);
    }

    protected virtual TargetMember[] getPotentialMatchingMembers(ShimMember shimMember)
    {
        var members = Symbol.GetAllMembers()
            .Select(m => TargetMember.Parse(m, this))
            .OfType<TargetMember>();
        return members.Where(shimMember.IsMatch).ToArray();
    }

    protected TargetMember[] getMatchingMembers(ShimMember shimMember)
    {
        // TODO: Property -> Methods

        var members = getPotentialMatchingMembers(shimMember);

        if (shimMember.ReturnType == null)
        {
            // No return type to compare further; take first match
            return members.Take(1).ToArray();
        }

        var matches = members.ToArray();
        if (!matches.Any())
        {
            // No matches
            return matches;
        }

        // Perfect match
        var matchMembers = members.Where(m => shimMember.ReturnType!.IsMatch(m.ReturnType)).ToArray();
        if (matchMembers.Any())
        {
            return [matchMembers[0]];
        }

        // Shimmable
        matchMembers = members.Where(m => m.IsShimmableReturnType(shimMember)).ToArray();
        if (matchMembers.Any())
        {
            return [matchMembers[0]];
        }

        // Fallback to highest in hierarchy
        return [matches[0]];
    }
}
