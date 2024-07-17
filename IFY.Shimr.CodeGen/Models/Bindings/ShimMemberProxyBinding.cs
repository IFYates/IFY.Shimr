using IFY.Shimr.CodeGen.Models.Members;

namespace IFY.Shimr.CodeGen.Models.Bindings;

/// <summary>
/// A binding between a <see cref="ShimMember"/> and a proxy member instead of <paramref name="target"/>.
/// </summary>
internal class ShimMemberProxyBinding(ShimMember shimMember, ShimTarget target, TargetMember proxyMember)
    : NullBinding(shimMember.Definition, target)
{
    public ShimMember ShimMember { get; } = shimMember;
    public TargetMember ProxyMember { get; } = proxyMember;

    public override void GenerateCode(StringBuilder code)
    {
        switch (ShimMember)
        {
            case ShimMember.ShimMethodMember methodMember:
                writeMethod(code, methodMember);
                break;
            case ShimMember.ShimPropertyMember propertyMember:
                writeProperty(code, propertyMember);
                break;
            default:
                code.AppendLine("// TODO: ShimMemberProxyBinding.GenerateCode " + ShimMember.Type);
                break;
        }
    }

    private void writeMethod(StringBuilder code, ShimMember.ShimMethodMember methodMember)
    {
        var proxyMethod = (IParameterisedMember)ProxyMember;
        var proxyParams = methodMember.FirstParameterIsInstance(proxyMethod)
            ? new[] { "this" }.Concat(proxyMethod.Parameters.Skip(1).Select(p => p.GetTargetArgumentCode())).ToArray()
            : proxyMethod.Parameters.Select(p => p.GetTargetArgumentCode()).ToArray();

        code.Append($"            public ")
            .Append($"{ShimMember.ReturnType?.ToDisplayString() ?? "void"} {ShimMember.Name}(")
            .Append(string.Join(", ", methodMember.Parameters.Select(p => p.ToString())))
            .Append($") => {methodMember.GetMemberCallee(ProxyMember)}.{ProxyMember.Name}(")
            .Append(string.Join(", ", proxyParams))
            .Append($")")
            .Append(methodMember.GetShimCode(ProxyMember))
            .AppendLine(";");
    }

    private void writeProperty(StringBuilder code, ShimMember.ShimPropertyMember propertyMember)
    {
        code.Append($"            public {propertyMember.ReturnType?.ToDisplayString() ?? "void"} {ShimMember.Name} {{");

        var callee = $"{ShimMember.GetMemberCallee(ProxyMember)}.{ProxyMember.Name}";
        if (propertyMember.IsGet)
        {
            code.Append($" get => {callee}{propertyMember.GetShimCode(ProxyMember)};");
        }
        if (propertyMember.IsSet)
        {
            code.Append($" set => {callee} = value{propertyMember.GetUnshimCode(ProxyMember)};");
        }
        if (propertyMember.IsInit)
        {
            code.Append($" init => {callee} = value{propertyMember.GetUnshimCode(ProxyMember)};");
        }

        code.AppendLine(" }");
    }
}
