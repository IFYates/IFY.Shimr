using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// A model of a shimmed type.
/// </summary>
internal class ShimTarget(ITypeSymbol symbol)
{
    public INamedTypeSymbol Symbol { get; } = (INamedTypeSymbol)symbol;
    public string FullTypeName { get; } = symbol.ToFullName();
    public string Name { get; } = $"{symbol.ToFullName().Hash()}_{symbol.Name}";
    public bool IsValueType { get; } = symbol.IsValueType;

    /// <summary>
    /// Find the <see cref="TargetMember"/>s in this <see cref="ShimTarget"/> that could be bound to the given <see cref="ShimMember"/>.
    /// </summary>
    public virtual TargetMember[] GetMatchingMembers(ShimMember shimMember, CodeErrorReporter errors)
    {
        var members = getMatchingMembers(shimMember);

        var proxyBehaviour = shimMember.Proxy?.Behaviour ?? ProxyBehaviour.Override; // Non-proxy is an override
        if (!members.Any() && proxyBehaviour == ProxyBehaviour.Override)
        {
            // TODO: optional, as per 'IgnoreMissingMembers'
            Diag.WriteOutput($"//// No match: {FullTypeName}.{shimMember.TargetName} for {shimMember.Type} {shimMember.Definition.FullTypeName}.{shimMember.Name} [ProxyBehaviour.{proxyBehaviour}]");
            errors.NoMemberError(Symbol, shimMember.Symbol);
            return [];
        }
        else if (members.Any() && proxyBehaviour == ProxyBehaviour.Add)
        {
            // TODO: Error that proxy is adding existing method
            errors.CodeGenError(new Exception($"[ProxyBehaviour.{proxyBehaviour}] {Symbol}, {shimMember.Symbol}"));
            return [];
        }

        return members;
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
