using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Members;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Models the target of a shimterface.
/// </summary>
internal class ShimClassTarget(BaseShimType shimterface, ITypeSymbol underlyingType) : IShimTarget
{
    public BaseShimType ShimType { get; } = shimterface;
    public string InterfaceFullName => ShimType.InterfaceFullName;
    public ITypeSymbol UnderlyingType { get; } = underlyingType;
    public string UnderlyingFullName { get; } = underlyingType.ToDisplayString(NullableFlowState.NotNull);

    public virtual string Name { get; } = $"{shimterface.InterfaceType.Name}_{underlyingType.Name}";

    public virtual void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        code.AppendLine($"        protected class {Name} : {InterfaceFullName}, IShim")
            .AppendLine("        {");

        // Constructor and Unshim
        code.AppendLine($"            private readonly {UnderlyingFullName} _inst;")
            .AppendLine($"            public {Name}({UnderlyingFullName} inst) => _inst = inst;")
            .AppendLine("            object IShim.Unshim() => _inst;");

        // Add ToString(), if not already
        var members = ShimType.ResolveShimMembers();
        if (!members.OfType<ShimMemberMethod>().Any(m => m.Name == nameof(ToString) && m.Parameters.Length == 0))
        {
            code.AppendLine("            public override string ToString() => _inst.ToString();");
        }

        // Shim'd members
        foreach (var member in members)
        {
            member.GenerateCode(code, errors, UnderlyingType);
        }

        code.AppendLine("        }");
    }

    /// <summary>
    /// Looks for additional shims required to complete shim.
    /// </summary>
    public void ResolveImplicitShims(ShimRegister shimRegister)
    {
        var members = ShimType.ResolveShimMembers();
        foreach (var member in members)
        {
            member.ResolveImplicitShims(shimRegister, this);
        }

        // Argument overrides
        foreach (var param in members.OfType<ShimMemberMethod>()
            .SelectMany(m => m.Parameters).Where(p => p.UnderlyingType != null))
        {
            shimRegister.GetOrCreate(param.Type)
                .AddShim(param.UnderlyingType!);
        }
    }
}
