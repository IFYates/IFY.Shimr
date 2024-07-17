using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimProxyTarget(ITypeSymbol symbol, ShimTarget shimTarget)
    : ShimTarget(symbol)
{
    public ShimTarget ShimTarget { get; } = shimTarget;

    public override TargetMember[] GetMatchingMembers(ShimMember shimMember, CodeErrorReporter errors)
    {
        var (proxyType, proxyTargetName, behaviour) = shimMember.Proxy!.Value;
        var members = getMatchingMembers(shimMember, true, proxyTargetName);
        if (!members.Any())
        {
            // TODO: optional, as per 'IgnoreMissingMembers'
            Diag.WriteOutput($"//// No proxy match: {proxyType.ToFullName()}.{proxyTargetName ?? shimMember.Name} for {shimMember.Definition.FullTypeName}.{shimMember.Name} [ProxyBehaviour.{behaviour}]");
            errors.NoMemberError(Symbol, shimMember.Symbol);
            return [];
        }

        return members;
    }
}
