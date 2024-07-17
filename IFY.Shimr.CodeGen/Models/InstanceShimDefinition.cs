using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class InstanceShimDefinition : IShimDefinition
{
    private readonly Dictionary<string, ShimTarget> _targets = [];
    private readonly ShimMember[] _members;

    public string FullTypeName { get; }
    public string Name { get; }

    public InstanceShimDefinition(ITypeSymbol symbol)
    {
        _members = symbol.GetAllMembers()
            .Select(m => ShimMember.Parse(m, this))
            .OfType<ShimMember>().ToArray();

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

    public void WriteShimClass(ICodeWriter writer, IEnumerable<IBinding> bindings)
    {
        var code = new StringBuilder();
        code.AppendLine($"namespace {GlobalCodeWriter.EXT_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    using System.Linq;")
            .AppendLine($"    public static partial class {GlobalCodeWriter.EXT_CLASSNAME}")
            .AppendLine("    {");

        // Each target has a class
        var targetBindings = bindings
            .GroupBy(b => b.Target.FullTypeName).ToArray();
        foreach (var group in targetBindings)
        {
            var className = group.First().ClassName;
            var interfaceType = group.First().Definition.FullTypeName;
            var implType = group.First().Target.FullTypeName;

            code.AppendLine($"        protected class {className} : {interfaceType}, IShim")
                .AppendLine("        {");

            // Constructor and Unshim
            code.AppendLine($"            private readonly {implType} _inst;")
                .AppendLine($"            public {className}({implType} inst) => _inst = inst;")
                .AppendLine("            object IShim.Unshim() => _inst;");

            // Add ToString(), if not already
            if (!_members.OfType<IParameterisedMember>().Any(m => m.Type == MemberType.Method && m.Name == nameof(ToString) && m.Parameters.Length == 0))
            {
                code.AppendLine("            public override string ToString() => _inst.ToString();");
            }

            // Members
            foreach (var binding in group)
            {
                binding.GenerateCode(code);
            }

            code.AppendLine("        }");
        }

        code.AppendLine("    }")
            .AppendLine("}");
        writer.AddSource($"Shimr.{Name}.g.cs", code);
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
