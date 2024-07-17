using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// A model of a shimmed type.
/// </summary>
internal class ShimTarget(ITypeSymbol symbol)
{
    public ITypeSymbol Symbol { get; } = symbol;
    public string FullTypeName { get; } = symbol.ToFullName();
    public string Name { get; } = $"{symbol.ToFullName().Hash()}_{symbol.Name}";
    public bool IsValueType { get; } = symbol.IsValueType;

    /// <summary>
    /// Find the <see cref="TargetMember"/>s in this <see cref="ShimTarget"/> that could be bound to the given <see cref="ShimMember"/>.
    /// </summary>
    public virtual TargetMember[] GetMatchingMembers(ShimMember shimMember, CodeErrorReporter errors)
    {
        var members = getMatchingMembers(shimMember, false, null);

        var proxyBehaviour = shimMember.Proxy?.Behaviour ?? ProxyBehaviour.Override; // Non-proxy is an override
        if (!members.Any() && proxyBehaviour == ProxyBehaviour.Override)
        {
            // TODO: optional, as per 'IgnoreMissingMembers'
            Diag.WriteOutput($"//// No match: {FullTypeName}.{shimMember.TargetName} for {shimMember.Definition.FullTypeName}.{shimMember.Name} [ProxyBehaviour.{proxyBehaviour}]");
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

    protected TargetMember[] getMatchingMembers(ShimMember shimMember, bool asProxy, string? proxyMemberName)
    {
        // TODO: Property -> Methods

        var members = Symbol.GetAllMembers()
            .Select(m => TargetMember.Parse(m, this))
            .OfType<TargetMember>();

        if (asProxy)
        {
            members = members.Where(m => m.Name == (proxyMemberName ?? shimMember.Name));

            // If proxy of method, may need to ignore first parameter
            if (shimMember is IParameterisedMember shimMethod)
            {
                members = members.Where(m =>
                {
                    if (m is not IParameterisedMember targetMethod
                        || targetMethod.Parameters.Length != shimMethod.Parameters.Length + 1
                        || !targetMethod.Parameters[0].Type.IsMatch(shimMember.ContainingType))
                    {
                        return shimMember.IsMatch(m);
                    }

                    Diag.WriteOutput($"///// Ignoring first parameter of {shimMethod.ContainingType.ToFullName()}.{shimMethod.Name}");
                    return shimMethod.Parameters.Select(isParameterMatch).All(v => v);
                    bool isParameterMatch(MemberParameter param1, int idx)
                        => (param1.UnderlyingType ?? param1.Type).IsMatch(targetMethod.Parameters[idx + 1].Type);
                });
            }
        }
        else
        {
            members = members.Where(shimMember.IsMatch);
        }

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
            return matchMembers;
        }

        // Shimmable
        matchMembers = members.Where(m => m.IsShimmableReturnType(shimMember)).ToArray();
        if (matchMembers.Any())
        {
            return matchMembers;
        }

        // Fallback to highest in hierarchy
        return [matches[0]];
    }
}
