using IFY.Shimr.CodeGen.Models.Members;

namespace IFY.Shimr.CodeGen.Models.Bindings;

/// <summary>
/// A binding between a <see cref="ShimMember"/> and <see cref="TargetMember"/>.
/// </summary>
internal class ShimMemberBinding(ShimMember shimMember, TargetMember targetMember, ShimTarget? target = null)
    : NullBinding(shimMember.Definition, target ?? targetMember.Target)
{
    public ShimMember ShimMember { get; } = shimMember;
    public TargetMember TargetMember { get; } = targetMember;

    public override void GenerateCode(StringBuilder code)
    {
        ShimMember.GenerateCode(code, TargetMember);
    }
}
