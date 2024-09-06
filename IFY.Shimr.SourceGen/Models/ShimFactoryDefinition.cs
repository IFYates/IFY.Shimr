using IFY.Shimr.SourceGen.CodeAnalysis;
using IFY.Shimr.SourceGen.Models.Bindings;
using IFY.Shimr.SourceGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen.Models;

internal class ShimFactoryDefinition : IShimDefinition
{
    public INamedTypeSymbol Symbol { get; }
    public string FullTypeName { get; }
    public string Name { get; }
    public ShimTarget? StaticTarget { get; }
    public ShimMember[] Members { get; }

    public ShimFactoryDefinition(ITypeSymbol symbol)
    {
        var staticAttr = symbol.GetAttribute<StaticShimAttribute>();
        if (staticAttr != null)
        {
            StaticTarget = new(StaticShimAttribute.GetArgument(staticAttr)!);
        }

        Symbol = (INamedTypeSymbol)symbol;
        FullTypeName = symbol.ToFullName();
        Name = $"ShimFactory__{symbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Hash()}_{symbol.ToClassName().Replace('.', '_')}";

        var members = symbol.GetAllMembers();
        Members = members.Select(m => ShimMember.Parse(m, this, members))
            .OfType<ShimMember>().ToArray();
    }

    public void SetMemberType(ISymbol symbol, ITypeSymbol target)
    {
        Members.Single(m => m.Symbol.Equals(symbol, SymbolEqualityComparer.Default))
            .TargetType = new(target);
    }

    private const string OUTER_CLASS_PRE_CS = $@"namespace {GlobalCodeWriter.SB_NAMESPACE}
{{{{
    using {GlobalCodeWriter.EXT_NAMESPACE};
    using System.Linq;
    internal static partial class {GlobalCodeWriter.SB_CLASSNAME}
    {{{{
        protected class {{0}} : {{1}}
        {{{{
";
    private const string OUTER_CLASS_POST_CS = @"        }
    }
}
";

    public void WriteShimClass(ICodeWriter writer, IEnumerable<IBinding> bindings)
    {
        writer.AppendFormat(OUTER_CLASS_PRE_CS, Name, FullTypeName);

        foreach (var binding in bindings)
        {
            binding.GenerateCode(writer);
        }

        writer.Append(OUTER_CLASS_POST_CS);
        writer.WriteSource($"Shimr.{Name}.g.cs");
    }

    public void Resolve(IList<IBinding> allBindings, CodeErrorReporter errors, ShimResolver shimResolver)
    {
        // Map shim members against targets
        foreach (var member in Members)
        {
            var target = member.TargetType ?? StaticTarget;
            if (target == null)
            {
                // TODO: error
                Diag.WriteOutput($"//// No static target {Name} {member.Name}");
                continue;
            }

            var targetMembers = target.GetMatchingMembers(member, errors);
            foreach (var targetMember in targetMembers)
            {
                member.ResolveBindings(allBindings, targetMember, errors, shimResolver);
            }
        }
    }
}
