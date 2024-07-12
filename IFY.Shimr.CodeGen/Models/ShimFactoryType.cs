using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimFactoryType(ITypeSymbol interfaceType) : BaseShimType(interfaceType)
{
    public override string Name { get; } = $"{interfaceType.Name}_Factory";

    public ShimFactoryTarget AddShim(ITypeSymbol underlyingType, ISymbol? singleMember)
    {
        var shim = new ShimFactoryTarget(this, underlyingType, singleMember, false);
        return AddTarget(shim);
    }
    public ShimFactoryTarget AddConstructor(ITypeSymbol underlyingType, IMethodSymbol constructorMethod)
    {
        var shim = new ShimFactoryTarget(this, underlyingType, constructorMethod, true);
        return AddTarget(shim);
    }

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        code.AppendLine($"namespace {AutoShimCodeWriter.SB_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    using {AutoShimCodeWriter.EXT_NAMESPACE};")
            .AppendLine($"    using System.Linq;")
            .AppendLine($"    public static partial class {AutoShimCodeWriter.SB_CLASSNAME}")
            .AppendLine("    {")
            .AppendLine($"        protected class {Name} : {InterfaceFullName}")
            .AppendLine("        {");

        var members = ResolveShimMembers().ToList();
        foreach (var memberTarget in Shims.OfType<ShimFactoryTarget>().Where(t => t.SingleMember != null))
        {
            var member = members.First(m => m.Symbol.Equals(memberTarget.SingleMember, SymbolEqualityComparer.Default));
            members.Remove(member);
            writeMemberCode(memberTarget, member);
        }

        var target = Shims.OfType<ShimFactoryTarget>().FirstOrDefault(t => t.SingleMember == null);
        if (target != null)
        {
            foreach (var member in members)
            {
                writeMemberCode(target, member);
            }
        }

        code.AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}");

        void writeMemberCode(ShimFactoryTarget target, IShimMember member)
        {
            if (target.IsConstructor)
            {
                // TODO: Find target constructor, or error

                var method = (ShimMemberMethod)member;
                code.Append($"            public {method.ReturnTypeName} {member.Name}(")
                    .Append(string.Join(", ", method.Parameters.Select(p => p.ToString())))
                    .Append(")");

                code.Append($" => new {target.UnderlyingFullName}(")
                    .Append(string.Join(", ", method.Parameters.Select(p => p.GetTargetArgumentCode())))
                    .Append($")")
                    .Append(method.GetShimCode(target.UnderlyingType))
                    .AppendLine(";");
            }
            else
            {
                member.GenerateCode(code, errors, target.UnderlyingType);
            }
        }
    }
}