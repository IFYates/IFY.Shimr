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

    /// <summary>
    /// Looks for additional shims required to complete shim.
    /// </summary>
    public void ResolveImplicitShims(ShimRegister shimRegister, IList<IShimTarget> shims)
    {
        var members = ShimType.ResolveShimMembers();

        // Return types
        foreach (var member in members.OfType<IReturnableShimMember>())
        {
            var underlyingReturn = member.GetUnderlyingMemberReturn(UnderlyingType);
            if (!underlyingReturn.IsMatch(member.ReturnType)
                && member.ReturnType.TypeKind == TypeKind.Interface)
            {
                shims.Add(shimRegister.GetOrCreate(member.ReturnType)
                    .AddShim(underlyingReturn));
            }
        }

        // Argument overrides
        foreach (var param in members.OfType<ShimMemberMethod>()
            .SelectMany(m => m.Parameters).Where(p => p.UnderlyingType != null))
        {
            shims.Add(shimRegister.GetOrCreate(param.Type)
                .AddShim(param.UnderlyingType!));
        }
    }

    public virtual void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        code.AppendLine($"        protected class {Name} : {InterfaceFullName}, IShim")
            .AppendLine("        {");

        // Constructor and Unshim
        code.AppendLine($"            protected readonly {UnderlyingFullName} _inst;")
            .AppendLine($"            public {Name}({UnderlyingFullName} inst) => _inst = inst;")
            .AppendLine("            public object Unshim() => _inst;");

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
}
