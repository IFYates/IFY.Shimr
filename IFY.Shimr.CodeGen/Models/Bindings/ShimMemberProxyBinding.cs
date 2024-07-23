using IFY.Shimr.CodeGen.Models.Members;

namespace IFY.Shimr.CodeGen.Models.Bindings;

/// <summary>
/// A binding between a <see cref="Members.ShimMember"/> and a proxy member instead of an actual <paramref name="target"/> member.
/// </summary>
/// <param name="shimMember">The shim that this proxy is a member of.</param>
/// <param name="target">The shim target.</param>
/// <param name="proxyMember">The proxy implementation to use.</param>
/// <param name="targetMember">The overridden member of the <see cref="ShimTarget"/>.</param>
internal class ShimMemberProxyBinding(ShimMember shimMember, ShimTarget target, TargetMember proxyMember, TargetMember? targetMember)
    : ShimMemberBinding(shimMember, proxyMember, target)
{
    private const string PROXY_MEMBER_RECURSIVE_CS = @"            private static readonly System.Threading.ThreadLocal<bool> _isRecursing{1} = new();
            public {0} {1}({2})
            {{
                if (_isRecursing{1}.Value)
                {{
                    {10}(({9})((IShim)this).Unshim()).{12}({7}){8};
                    return{11};
                }}
                _isRecursing{1}.Value = true;
                try
                {{
                    {10}{3}.{4}({5}{6}{7}){8};
                }}
                finally
                {{
                    _isRecursing{1}.Value = false;
                }}
                return{11};
            }}";
    private const string PROXY_MEMBER_CS = @"            public {0} {1}({2}) => {3}.{4}({5}{6}{7}){8};";

    public override void GenerateCode(StringBuilder code)
    {
        switch (ShimMember)
        {
            case ShimMember.ShimMethodMember methodMember:
                writeMethod(code, methodMember);
                break;
            case ShimMember.ShimPropertyMember:
                ShimMember.GenerateCode(code, TargetMember);
                break;
            default:
                code.AppendLine("// TODO: ShimMemberProxyBinding.GenerateCode " + ShimMember.Type);
                break;
        }
    }

    private void writeMethod(StringBuilder code, ShimMember.ShimMethodMember methodMember)
    {
        var proxyMethod = (IParameterisedMember)TargetMember;
        var hasThisParam = methodMember.FirstParameterIsInstance(proxyMethod);
        var proxyParams = hasThisParam ? proxyMethod.Parameters.Skip(1) : proxyMethod.Parameters;
        var isVoid = ShimMember.ReturnType?.ToDisplayString() is null or "void";

        var codeArgs = new[]
        {
            !isVoid ? ShimMember.ReturnType?.ToDisplayString() : "void",
            ShimMember.Name,
            string.Join(", ", methodMember.Parameters.Select(p => p.ToString())),
            methodMember.GetMemberCallee(TargetMember),
            TargetMember.Name,
            hasThisParam ? "this" : null,
            hasThisParam && proxyParams.Any() ? ", " : null,
            string.Join(", ", proxyParams.Select(p => p.GetTargetArgumentCode()).ToArray()),
            methodMember.GetShimCode(TargetMember),
            Target.FullTypeName,
            !isVoid ? "var result = " : null,
            !isVoid ? " result" : null,
            targetMember?.Name ?? ShimMember.Name
        };

        // TODO: ICodeWriter.HasStackTraceHiddenAttribute
        //code.AppendLine("            [System.Diagnostics.StackTraceHiddenAttribute]");

        if (ShimMember.Proxy!.Value.Behaviour is ProxyBehaviour.Graceful or ProxyBehaviour.Override
            && targetMember != null && hasThisParam
            && targetMember.IsShimmableReturnType(TargetMember))
        {
            code.AppendFormat(PROXY_MEMBER_RECURSIVE_CS, codeArgs)
                .AppendLine();
        }
        else
        {
            code.AppendFormat(PROXY_MEMBER_CS, codeArgs)
                .AppendLine();
        }
    }
}
