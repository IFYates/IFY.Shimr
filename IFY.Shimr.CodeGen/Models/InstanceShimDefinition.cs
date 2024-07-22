using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

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

    private const string OUTER_CLASS_CS = $@"namespace {GlobalCodeWriter.EXT_NAMESPACE}
{{{{
    using System.Linq;
    public static partial class {GlobalCodeWriter.EXT_CLASSNAME}
    {{{{
{{0}}
    }}}}
}}}}
";
    private const string CLASS_CS = $@"         protected class {{0}}{{1}} : {{2}}, IShim{{3}}
         {{{{
            private readonly {{4}} _inst;
            public {{0}}({{4}} inst) => _inst = inst;
            object IShim.Unshim() => _inst;
{{5}}         }}}}";

    public void WriteShimClass(ICodeWriter writer, IEnumerable<IBinding> bindings)
    {
        var code = new StringBuilder();

        // Each target has a class
        var targetBindings = bindings
        .GroupBy(b => b.Target.FullTypeName).ToArray();
        foreach (var group in targetBindings)
        {
            // Add ToString(), if not already
            var classCode = new StringBuilder();
            if (!Members.OfType<ShimMember.ShimMethodMember>()
                .Any(m => m.Name == nameof(ToString) && m.Parameters.Length == 0))
            {
                classCode.AppendLine("            public override string ToString() => _inst.ToString();");
            }

            // Members
            foreach (var binding in group)
            {
                binding.GenerateCode(classCode);
            }

            var symbol = group.First().Definition.Symbol;
            var codeArgs = new[]
            {
                group.First().ClassName,
                symbol.TypeParameters.ToTypeParameterList(),
                group.First().Definition.FullTypeName, // Interface
                symbol.TypeParameters.ToWhereClause(),
                group.First().Target.FullTypeName, // Implementation
                classCode.ToString()
            };
            code.AppendFormat(CLASS_CS, codeArgs);
        }

        writer.AddSource($"Shimr.{Name}.g.cs", string.Format(OUTER_CLASS_CS, code.ToString()));
    }

    public void Resolve(IList<IBinding> allBindings, CodeErrorReporter errors, ShimResolver shimResolver)
    {
        // Map shim members against targets
        foreach (var target in _targets.Values)
        {
            allBindings.Add(new NullBinding(this, target)); // Ensure empty shim generated

            foreach (var member in Members)
            {
                var targetMembers = target.GetMatchingMembers(member, errors);

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
                        Diag.WriteOutput($"//// No match: {FullTypeName}.{member.TargetName} for {member.Type} {member.Definition.FullTypeName}.{member.Name}");
                        errors.NoMemberError(Symbol, member.Symbol);
                    }
                    continue;
                }

                foreach (var targetMember in targetMembers)
                {
                    member.ResolveBindings(allBindings, targetMember, errors, shimResolver);
                }
            }
        }
    }
}
