using IFY.Shimr.Gen.SyntaxParsing;
using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.Model;

internal abstract class ShimMember
{
    public ISymbol Symbol { get; protected set; } = null!;
    // Unique signature
    public string SignatureName { get; protected set; } = null!;

    // The full name of the interface that defines this member, if explicit reference needed
    public string? ExplicitInterfaceType { get; set; }

    // The implementation target
    public ISymbol? TargetMember { get; set; }
    // The full name of the target type that provides the implementation
    public string? TargetType { get; set; }
    // The full name of the return type of the target member, if different to the shim
    public string? TargetReturnType { get; set; }
    // The name of the target member, if different to the shim member
    public string? TargetName { get; set; }

    public bool IsStatic { get; set; }
    public ITypeSymbol? StaticType { get; set; }

    public string ReturnType { get; set; } = null!;
    public string Name { get; set; } = null!;

    protected string GetMemberStart()
    {
        return ExplicitInterfaceType == null
            ? $"public {ReturnType ?? "void"} {Name}"
            : $"{ReturnType} {ExplicitInterfaceType}.{Name}";
    }
}

internal class ShimMethodMember : ShimMember
{
    new public IMethodSymbol Symbol => (IMethodSymbol)base.Symbol;

    public ShimMethodMember(IMethodSymbol method, ShimTypeDefinition typeDef)
    {
        base.Symbol = method;

        if (method.TryGetReturnType(out var retType))
        {
            ReturnType = retType.FullName;
        }
        Name = method.Name;

        SignatureName = Name; // TODO: args

        // ShimProxyAttribute
        var proxyAttr = Symbol.GetAttribute<ShimProxyAttribute>();
        if (proxyAttr != null
            // implementationType is required
            && proxyAttr.TryGetAttributeConstructorValue("implementationType", out var proxyTypeArg)
            && proxyTypeArg != null)
        {
            StaticType = (ITypeSymbol)proxyTypeArg;
        }

        // ConstructorShimAttribute
        var constrAttr = Symbol.GetAttribute<ConstructorShimAttribute>();
        if (constrAttr != null)
        {
            if (proxyAttr != null)
            {
                Symbol.ReportConflictingAttributes(Symbol.FullName(), Name, typeof(ShimProxyAttribute).FullName, typeof(StaticShimAttribute).FullName);
            }
            else
            {
                IsConstructor = true;
                IsStatic = true;
                constrAttr.TryGetAttributeConstructorValue("targetType", out var constrTargetType);
                StaticType = constrTargetType != null
                    ? (ITypeSymbol)constrTargetType!
                    : typeDef.StaticType;
                TargetReturnType = StaticType!.TryFullName();
                //IsReturnShim = true;
            }
        }
    }

    public bool IsConstructor { get; set; }

    // TODO: gen args

    public string GetGenArgs()
    {
        return string.Empty;
    }
    public string GetParams()
    {
        return string.Join(", ", Symbol.Parameters
            .Select(p => string.Format("{0} {1}", p.Type.TryFullName(), p.Name)));
    }
    public override string ToString()
    {
        var objName = !IsStatic
            ? "_obj"
            : StaticType!.TryFullName();

        string getValue()
        {
            var args = string.Join(", ", Symbol.Parameters.Select(p => p.Name));
            var str = !IsConstructor
                ? $"{objName}.{TargetName ?? Name}({args})"
                : $"new {objName}{GetGenArgs()}({args})";
            if (ReturnType != null && TargetReturnType != null && ReturnType != TargetReturnType)
            {
                str += $".Shim<{ReturnType}>()";
            }
            return str;
        }

        var str = $"{GetMemberStart()}{GetGenArgs()}({GetParams()})\n"
            + "{\n";
        return str
            + (ReturnType != null ? "\treturn " : "\t")
            + $"{getValue()};\n}}";
    }
}

internal class ShimPropertyMember : ShimMember
{
    new public IPropertySymbol Symbol => (IPropertySymbol)base.Symbol;

    public ShimPropertyMember(IPropertySymbol property, ShimTypeDefinition typeDef)
    {
        base.Symbol = property;

        if (property.TryGetReturnType(out var retType))
        {
            ReturnType = retType.FullName;
        }
        Name = property.Name;
        SignatureName = Name;

        CanRead = property.GetMethod != null;
        CanWrite = property.SetMethod != null;
    }

    // The full name of the property type
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }

    // If the target of this shim is a set of methods (get_, set_)
    public bool UseMethods { get; set; }

    public override string ToString()
    {
        var objName = "_obj";
        string getValue()
        {
            return $"{objName}.{TargetName ?? Name}"
                + (UseMethods ? "()" : "");
        }

        var res = GetMemberStart();
        if (CanRead && !CanWrite)
        {
            res += $"\n\t=> {getValue()};";
        }
        else
        {
            res += "\n{";
            if (CanRead)
            {
                res += $"\n\tget => {getValue()};";
            }
            if (CanWrite)
            {
                var value = "value";

                res += $"\n\tset => {objName}.{TargetName ?? Name}";
                res += UseMethods ? $"({value});" : $" = {value};";
            }
            res += "\n}";
        }

        // etc.
        return res;
    }
}