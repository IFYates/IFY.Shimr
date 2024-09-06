using IFY.Shimr.SourceGen.CodeAnalysis;
using IFY.Shimr.SourceGen.Models.Bindings;
using IFY.Shimr.SourceGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen.Models;

internal class InstanceShimDefinition : IShimDefinition
{
    private readonly Dictionary<string, ShimTarget> _targets = [];

    public INamedTypeSymbol Symbol { get; }
    public string FullTypeName { get; }
    public string Name { get; }
    public ShimMember[] Members { get; }

    public InstanceShimDefinition(ITypeSymbol symbol)
    {
        var members = symbol.GetAllMembers();
        Members = members.Select(m => ShimMember.Parse(m, this, members))
            .OfType<ShimMember>().ToArray();

        Symbol = (INamedTypeSymbol)symbol;
        FullTypeName = symbol.ToFullName();
        Name = $"{symbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Hash()}_{symbol.ToClassName().Replace('.', '_')}";
    }

    public ShimTarget AddTarget(ITypeSymbol symbol)
    {
        lock (_targets)
        {
            var key = symbol.ToDisplayString();
            if (!_targets.TryGetValue(key, out var target))
            {
                target = new(symbol);
                _targets.Add(key, target);
            }
            return target;
        }
    }

    private const string OUTER_CLASS_PRE_CS = $@"namespace {GlobalCodeWriter.EXT_NAMESPACE}
{{
    using System.Linq;
    internal static partial class {GlobalCodeWriter.EXT_CLASSNAME}
    {{
";
    private const string OUTER_CLASS_POST_CS = @"
    }
}
";
    private const string CLASS_PRE_CS = @"         protected class {0}{1} : {2}, IShim{3}
         {{
            private readonly {4} _inst;
            public {0}({4} inst) => _inst = inst;
            object IShim.Unshim() => _inst;
";
    private const string CLASS_POST_CS = @"         }";

    public void WriteShimClass(ICodeWriter writer, IEnumerable<IBinding> bindings)
    {
        writer.Append(OUTER_CLASS_PRE_CS);

        // Each target has a class
        var targetBindings = bindings.GroupBy(b => b.Target.FullTypeName).ToArray();
        foreach (var group in targetBindings)
        {
            var symbol = group.First().Definition.Symbol;
            var codeArgs = new[]
            {
                group.First().ClassName,
                symbol.TypeParameters.ToTypeParameterList(),
                group.First().Definition.FullTypeName, // Interface
                symbol.TypeParameters.ToWhereClause(),
                group.First().Target.FullTypeName // Implementation
            };
            writer.AppendFormat(CLASS_PRE_CS, codeArgs);

            // Add ToString(), if not already
            if (!Members.OfType<ShimMember.ShimMethodMember>()
                .Any(m => m.Name == nameof(ToString) && m.Parameters.Length == 0))
            {
                writer.AppendLine("            public override string ToString() => _inst.ToString();");
            }

            // Members
            foreach (var binding in group)
            {
                binding.GenerateCode(writer);
            }

            writer.Append(CLASS_POST_CS);
        }

        writer.Append(OUTER_CLASS_POST_CS);
        writer.WriteSource($"Shimr.{Name}.g.cs");
    }

    public void Resolve(IList<IBinding> allBindings, CodeErrorReporter errors, ShimResolver shimResolver)
    {
        // Map shim members against targets
        foreach (var target in _targets.Values)
        {
            allBindings.Add(new NullBinding(this, target)); // Ensure empty shim generated

            foreach (var member in Members)
            {
                var memberTarget = member.TargetType ?? target;
                var targetMembers = memberTarget.GetMatchingMembers(member, errors);

                if (member.Proxy != null)
                {
                    // We need to complete target.GetMatchingMembers and then locate proxy
                    targetMembers = new ShimProxyTarget(member.Proxy.Value.ImplementationType, target, targetMembers)
                        .GetMatchingMembers(member, errors);
                }

                if (!targetMembers.Any())
                {
                    if (member.Proxy == null)
                    {
                        // TODO: optional, as per 'IgnoreMissingMembers'
                        // TODO: register NotImplemented binding
                        Diag.WriteOutput($"//// No match: {target.FullTypeName}.{member.TargetName} for {member.Type} {member.Definition.FullTypeName}.{member.Name}");
                        errors.NoMemberError(target.Symbol, member.Symbol);
                    }
                    continue;
                }

                foreach (var targetMember in targetMembers)
                {
                    member.ResolveBindings(allBindings, targetMember, errors, shimResolver, target);
                }
            }
        }
    }
}
