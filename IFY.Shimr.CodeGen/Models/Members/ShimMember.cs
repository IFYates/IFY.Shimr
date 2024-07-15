using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

/// <summary>
/// A shim member.
/// </summary>
internal abstract class ShimMember : IMember
{
    protected sealed class ShimFieldMember(IShimDefinition shim, IFieldSymbol symbol)
        : ShimMember(shim, symbol, MemberType.Field)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.Type;

        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            throw new NotImplementedException();
        }
    }

    protected sealed class ShimPropertyMember(IShimDefinition shim, IPropertySymbol symbol)
        : ShimMember(shim, symbol, MemberType.Property)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.Type;

        public bool IsGet { get; } = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public;
        public bool IsSet { get; } = symbol.SetMethod?.DeclaredAccessibility == Accessibility.Public && symbol.SetMethod?.IsInitOnly == false;
        public bool IsInit { get; } = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public && symbol.SetMethod?.IsInitOnly == true;

        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            code.Append($"            public {ReturnType?.ToDisplayString() ?? "void"} {Name} {{");

            var callee = GetMemberCallee(targetMember);
            if (IsGet)
            {
                code.Append($" get => {callee}.{OriginalName}{GetShimCode(targetMember)};");
            }
            if (IsSet)
            {
                code.Append($" set => {callee}.{OriginalName} = value{GetUnshimCode(targetMember)};");
            }
            if (IsInit)
            {
                code.Append($" init => {callee}.{OriginalName} = value{GetUnshimCode(targetMember)};");
            }

            code.AppendLine(" }");
        }
    }

    protected class ShimMethodMember(IShimDefinition shim, IMethodSymbol symbol, MemberType type = MemberType.Method)
        : ShimMember(shim, symbol, type), IParameterisedMember
    {
        // TODO
        public MemberParameter[] Parameters { get; }
            = symbol.Parameters.Select(p => new MemberParameter(p)).ToArray();

        public override ITypeSymbol? ReturnType { get; } = symbol.ReturnType;

        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            code.Append($"            public ");
            if (((IParameterisedMember)targetMember).Parameters.Length == 0 && Name is nameof(ToString) or nameof(GetHashCode))
            {
                code.Append("override ");
            }

            code.Append($"{ReturnType?.ToDisplayString() ?? "void"} {Name}(")
                .Append(string.Join(", ", Parameters.Select(p => p.ToString())))
                .Append(") => ")
                .Append(GetMemberCallee(targetMember))
                .Append($".{targetMember.Name}(")
                .Append(string.Join(", ", Parameters.Select(p => p.GetTargetArgumentCode())))
                .Append($")")
                .Append(GetShimCode(targetMember))
                .AppendLine(";");
        }

        public override bool IsMatch(ISymbol symbol)
        {
            return symbol is IMethodSymbol method
                && method.MethodKind is MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation
                && method.Name == OriginalName
                && method.Parameters.Length == Parameters.Length
                && method.AllParameterTypesMatch((IMethodSymbol)Symbol);
        }

        public override void ResolveBinding(IList<IBinding> bindings, TargetMember target, CodeErrorReporter errors, ShimRegister shimRegister)
        {
            // Resolve parameter overrides
            foreach (var param in Parameters)
            {
                param.RegisterOverride(shimRegister);
            }

            base.ResolveBinding(bindings, target, errors, shimRegister);
        }
    }

    protected sealed class ShimConstructorMember(IShimDefinition shim, IMethodSymbol symbol, ITypeSymbol targetType)
        : ShimMethodMember(shim, symbol, MemberType.Constructor), IParameterisedMember
    {
        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            code.Append($"            public {ReturnType?.ToDisplayString() ?? "void"} {Name}(")
                .Append(string.Join(", ", Parameters.Select(p => p.ToString())))
                .Append($") => new {targetType}(")
                .Append(string.Join(", ", Parameters.Select(p => p.GetTargetArgumentCode())))
                .Append($")")
                .Append(GetShimCode(targetMember))
                .AppendLine(";");
        }

        public override bool IsMatch(ISymbol symbol)
        {
            return symbol is IMethodSymbol method
                && method.MethodKind == MethodKind.Constructor
                && method.Parameters.Length == Parameters.Length
                && method.AllParameterTypesMatch((IMethodSymbol)Symbol);
        }
    }

    public static ShimMember? Parse(ISymbol symbol, IShimDefinition shim)
    {
        // Get constructor attribute
        ITypeSymbol? constructionType = null;
        if (shim is ShimFactoryDefinition factory)
        {
            var constructorAttr = symbol.GetAttribute<ConstructorShimAttribute>();
            if (constructorAttr != null)
            {
                constructionType = (ITypeSymbol?)constructorAttr?.ConstructorArguments.FirstOrDefault().Value
                    ?? factory.StaticTarget?.Symbol;
            }
        }

        // Build member
        return symbol switch
        {
            ISymbol { IsAbstract: false } => null,
            IFieldSymbol field => new ShimFieldMember(shim, field),
            IPropertySymbol property => new ShimPropertyMember(shim, property),
            // TODO: property.ExplicitInterfaceImplementations.Any()?
            IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.ExplicitInterfaceImplementation } => null,
            IMethodSymbol method => constructionType == null
                ? new ShimMethodMember(shim, method)
                : new ShimConstructorMember(shim, method, constructionType!),
            _ => throw new NotSupportedException($"Unhandled symbol type: {symbol.GetType().FullName}"),
        };
    }

    public IShimDefinition Definition { get; }
    public ISymbol Symbol { get; }
    public INamedTypeSymbol ContainingType { get; }
    public bool IsFactoryMember { get; }
    public string Name { get; }
    public MemberType Type { get; }
    public virtual ITypeSymbol? ReturnType { get; }
    public string OriginalName { get; }

    public ShimTarget? TargetType { get; set; }

    private ShimMember(IShimDefinition shim, ISymbol symbol, MemberType type)
    {
        Definition = shim;
        Symbol = symbol;
        ContainingType = symbol.ContainingType;
        IsFactoryMember = shim is ShimFactoryDefinition;
        Name = symbol.Name;
        Type = type;

        // Check for rename via attribute
        var attr = symbol.GetAttribute<ShimAttribute>();
        OriginalName = (attr?.ConstructorArguments.Length) switch // Could be (string), (Type), or (Type, string)
        {
            1 => attr.ConstructorArguments[0].Type!.IsType<string>()
                ? attr.ConstructorArguments[0].Value?.ToString() : null,
            2 => attr.ConstructorArguments[1].Value?.ToString(),
            _ => null
        } ?? Name;
    }

    public abstract void GenerateCode(StringBuilder code, TargetMember targetMember);

    protected string GetMemberCallee(TargetMember targetMember)
        => IsFactoryMember
        ? targetMember.ContainingType.ToFullName()
        : $"(({targetMember.ContainingType.ToFullName()})_inst)";
    protected string? GetShimCode(TargetMember target)
    {
        string? str = null;// $"/* {target.ReturnType.ToDisplayString()} */";
        if (ReturnType!.IsMatch(target.ReturnType))
        {
            return str;
        }

        // Array shim
        if (ReturnType!.IsEnumerable(out var returnElementType))
        {
            return $"{str}.Select(e => e.Shim<{returnElementType!.ToDisplayString()}>()).ToArray()";
        }

        // Only shim if it's an interface
        return ReturnType!.TypeKind != TypeKind.Interface ? str : $"{str}.Shim<{ReturnType.ToDisplayString()}>()";
    }
    protected string GetUnshimCode(TargetMember target)
        => ReturnType?.IsMatch(target.ReturnType) == false
        ? $".Unshim<{target.ReturnType!.ToDisplayString()}>()"
        : string.Empty;

    public virtual bool IsMatch(ISymbol symbol)
    {
        return symbol.Name == OriginalName;
    }

    public virtual void ResolveBinding(IList<IBinding> bindings, TargetMember target, CodeErrorReporter errors, ShimRegister shimRegister)
    {
        // Register return shim, if needed
        if (target.IsShimmableReturnType(this, out var targetElement, out var shimElement))
        {
            if (targetElement != null && shimElement != null)
            {
                safeRegister(shimElement, targetElement);
            }
        }
        else if (ReturnType!.TypeKind == TypeKind.Interface)
        {
            // Optimistic
            safeRegister(ReturnType, target.ReturnType!);
        }
        else
        {
            // Error no matching method
            errors.NoMemberError(target.ContainingType, Symbol);
            return;
        }

        bindings.Add(new ShimMemberBinding(this, target));

        void safeRegister(ITypeSymbol shimType, ITypeSymbol targetType)
        {
            var shim = shimRegister.GetOrCreate(shimType, false);
            if (shim is InstanceShimDefinition def)
            {
                def.AddTarget(targetType);
            }
            else
            {
                // TODO: Error for bad use of factory shim as instance shim
                errors.NoMemberError(target.ContainingType, Symbol);
            }
        }
    }
}
