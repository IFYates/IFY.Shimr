using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class InstanceShimDefinition : IShimDefinition
{
    private readonly Dictionary<string, ShimTarget> _targets = [];
    private readonly ShimMember[] _members;

    public INamedTypeSymbol Symbol { get; }
    public string FullTypeName { get; }
    public string Name { get; }

    public InstanceShimDefinition(ITypeSymbol symbol)
    {
        _members = symbol.GetAllMembers()
            .Select(m => ShimMember.Parse(m, this))
            .OfType<ShimMember>().ToArray();

        Symbol = (INamedTypeSymbol)symbol;
        FullTypeName = symbol.ToFullName();
        Name = $"{symbol.ToFullName().Hash()}_{symbol.Name}";
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
{{5}}
         }}}}";

    public void WriteShimClass(ICodeWriter writer, IEnumerable<IBinding> bindings)
    {
        var code = new StringBuilder();

        // Each target has a class
        var targetBindings = bindings
            .GroupBy(b => b.Target.FullTypeName).ToArray();
        foreach (var group in targetBindings)
        {
            var codeArgs = new string[6];
            codeArgs[0] = group.First().ClassName;
            codeArgs[2] = group.First().Definition.FullTypeName; // Interface
            codeArgs[4] = group.First().Target.FullTypeName; // Implementation

            var symbol = group.First().Definition.Symbol;
            if (symbol.IsGenericType)
            {
                codeArgs[1] = symbol.TypeParameters.ToTypeParameterList();
                codeArgs[3] = symbol.TypeParameters.ToWhereClause();
            }

            // Add ToString(), if not already
            var classCode = new StringBuilder();
            if (!_members.OfType<IParameterisedMember>().Any(m => m.Type == MemberType.Method && m.Name == nameof(ToString) && m.Parameters.Length == 0))
            {
                classCode.AppendLine("            public override string ToString() => _inst.ToString();");
            }

            // Members
            foreach (var binding in group)
            {
                binding.GenerateCode(classCode);
            }

            codeArgs[5] = classCode.ToString();
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

            foreach (var member in _members)
            {
                var targetMembers = target.GetMatchingMembers(member, errors);

                if (member.Proxy != null)
                {
                    // We need to complete target.GetMatchingMembers and then locate proxy
                    targetMembers = new ShimProxyTarget(member.Proxy.Value.ImplementationType, target)
                        .GetMatchingMembers(member, errors);
                }

                if (!targetMembers.Any())
                {
                    // TODO: register NotImplemented binding
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
