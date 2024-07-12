using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Models an interface-underlying shim combination.
/// </summary>
internal class ShimModel(ShimterfaceModel shimterface, ITypeSymbol underlyingType, string keySuffix = null!)
{
    public ShimterfaceModel Shimterface { get; } = shimterface;
    public string InterfaceFullName => Shimterface.InterfaceFullName;
    public ITypeSymbol UnderlyingType { get; } = underlyingType;
    public string UnderlyingFullName { get; } = underlyingType.ToDisplayString();

    public string Key { get; } = shimterface.InterfaceType.Name + "_" + underlyingType.Name + keySuffix;
    public string Name { get; }
        = shimterface.InterfaceType.Name + "_" + underlyingType.Name + keySuffix + "_" + ShimRegister.R.Next();

    /// <summary>
    /// Looks for additional shims required to complete shim.
    /// </summary>
    public void ResolveImplicitShims(ShimRegister shimRegister, IList<ShimModel> shims)
    {
        var members = Shimterface.ResolveShimMembers();

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
        foreach (var param in members.OfType<ShimMethod>()
            .SelectMany(m => m.Parameters).Where(p => p.UnderlyingType != null))
        {
            shims.Add(shimRegister.GetOrCreate(param.Type)
                .AddShim(param.UnderlyingType!));
        }
    }

    public void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        var isFactory = this is ShimFactoryModel;
        var members = Shimterface.ResolveShimMembers();

        code.Append($"        protected class {Name} : {InterfaceFullName}")
            .AppendLine(!isFactory ? ", IShim" : null)
            .AppendLine("        {");

        if (!isFactory)
        {
            // Constructor and Unshim
            code.AppendLine($"            protected readonly {UnderlyingFullName} _inst;")
                .AppendLine($"            public {Name}({UnderlyingFullName} inst) => _inst = inst;")
                .AppendLine("            public object Unshim() => _inst;");

            // Add ToString(), if not already
            if (!members.OfType<ShimMethod>().Any(m => m.Name == nameof(ToString) && m.Parameters.Length == 0))
            {
                code.AppendLine("            public override string ToString() => _inst.ToString();");
            }
        }

        // Shim'd members
        foreach (var member in members)
        {
            member.GenerateCode(code, errors, UnderlyingType);
        }

        code.AppendLine("        }");
    }
}
