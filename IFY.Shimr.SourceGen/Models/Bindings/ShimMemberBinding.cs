using IFY.Shimr.SourceGen.CodeAnalysis;
using IFY.Shimr.SourceGen.Models.Members;

namespace IFY.Shimr.SourceGen.Models.Bindings;

/// <summary>
/// A binding between a <see cref="ShimMember"/> and <see cref="TargetMember"/>.
/// </summary>
internal class ShimMemberBinding(ShimMember shimMember, TargetMember targetMember, ShimTarget? target = null)
    : NullBinding(shimMember.Definition, target ?? targetMember.Target)
{
    public ShimMember ShimMember { get; } = shimMember;
    public TargetMember TargetMember { get; } = targetMember;

    public override void GenerateCode(ICodeWriter writer)
    {
        var code = new StringBuilder();
        ShimMember.GenerateCode(code, this);
        writer.Append(code.ToString());
    }

    public string GetMemberCallee(ShimMember member)
    {
        var targetType = TargetMember.ContainingType.ToFullName();
        return member.IsFactoryMember || TargetMember.IsStatic
            ? targetType
            : targetType == Target.FullTypeName
            ? $"_inst"
            : $"(({targetType})_inst)";
    }
}
