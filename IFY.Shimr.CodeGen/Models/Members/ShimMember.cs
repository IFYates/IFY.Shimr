using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

/// <summary>
/// A shim member.
/// </summary>
internal abstract class ShimMember : IMember
{
    public sealed class ShimConstructorMember(IShimDefinition shim, IMethodSymbol symbol, ITypeSymbol targetType)
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

        public override bool IsMatch(IMember member)
        {
            return member.Symbol is IMethodSymbol method
                && method.MethodKind == MethodKind.Constructor
                && ((IMethodSymbol)Symbol).AllParameterTypesMatch(method.Parameters);
        }
    }

    public sealed class ShimEventMember(IShimDefinition shim, IEventSymbol symbol)
        : ShimMember(shim, symbol, MemberType.Event)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.Type;

        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            code.Append($"            public event {ReturnType?.ToDisplayString() ?? "void"} {Name} {{")
                .Append($" add {{ _inst.{TargetName} += value; }}")
                .Append($" remove {{ _inst.{TargetName} -= value; }}")
                .AppendLine(" }");
        }
    }

    public sealed class ShimIndexerMember(IShimDefinition shim, IPropertySymbol symbol)
        : ShimPropertyMember(shim, symbol, MemberType.Indexer)
    {
        public string IndexerArgType { get; } = symbol.Parameters[0].Type.ToString();
        public string IndexerArgName { get; } = symbol.Parameters[0].Name;

        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            code.Append($"            public {ReturnType?.ToDisplayString() ?? "void"} this[{IndexerArgType} {IndexerArgName}] {{");

            
            var callee = IsFactoryMember || targetMember.IsStatic
                ? $"{targetMember.ContainingType.ToFullName()}[{IndexerArgName}]"
                : $"(({targetMember.ContainingType.ToFullName()})_inst)[{IndexerArgName}]";
            if (IsGet)
            {
                code.Append($" get => {callee}{GetShimCode(targetMember)};");
            }
            if (IsSet)
            {
                code.Append($" set => {callee} = value{GetUnshimCode(targetMember)};");
            }
            if (IsInit)
            {
                code.Append($" init => {callee} = value{GetUnshimCode(targetMember)};");
            }

            code.AppendLine(" }");
        }
    }

    public class ShimMethodMember(IShimDefinition shim, IMethodSymbol symbol, MemberType type = MemberType.Method)
        : ShimMember(shim, symbol, type), IParameterisedMember
    {
        public MemberParameter[] Parameters { get; }
            = symbol.Parameters.Select(p => new MemberParameter(p)).ToArray();

        public override ITypeSymbol? ReturnType { get; } = symbol.ReturnType;

        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            code.Append($"            public ");
            if (((IParameterisedMember)targetMember).Parameters.Length == 0
                && Name is nameof(ToString) or nameof(GetHashCode))
            {
                code.Append("override ");
            }

            code.Append($"{ReturnType?.ToDisplayString() ?? "void"} {Name}(")
                .Append(string.Join(", ", Parameters.Select(p => p.ToString())))
                .Append($") => {GetMemberCallee(targetMember)}(")
                .Append(string.Join(", ", Parameters.Select(p => p.GetTargetArgumentCode())))
                .Append($")")
                .Append(GetShimCode(targetMember))
                .AppendLine(";");
        }

        public bool FirstParameterIsInstance(IParameterisedMember targetMethod)
        {
            return targetMethod.Parameters.Length == Parameters.Length + 1
                && targetMethod.Parameters[0].Type.IsMatch(ContainingType);
        }

        public override bool IsMatch(IMember member)
        {
            return member.Symbol is IMethodSymbol method
                && method.MethodKind is MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation
                && method.Name == TargetName
                && ((IMethodSymbol)Symbol).AllParameterTypesMatch(method.Parameters);
        }

        public override void ResolveBindings(IList<IBinding> bindings, TargetMember targetMember, CodeErrorReporter errors, ShimResolver shimResolver)
        {
            // Resolve parameter overrides
            foreach (var param in Parameters)
            {
                param.RegisterOverride(shimResolver);
            }

            base.ResolveBindings(bindings, targetMember, errors, shimResolver);
        }
    }

    public class ShimPropertyMember(IShimDefinition shim, IPropertySymbol symbol, MemberType type = MemberType.Property)
        : ShimMember(shim, symbol, type)
    {
        public override ITypeSymbol? ReturnType { get; } = symbol.Type;

        public bool IsGet { get; }
            = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public;
        public bool IsSet { get; }
            = symbol.SetMethod?.DeclaredAccessibility == Accessibility.Public
            && symbol.SetMethod?.IsInitOnly == false;
        public bool IsInit { get; }
            = symbol.GetMethod?.DeclaredAccessibility == Accessibility.Public
            && symbol.SetMethod?.IsInitOnly == true;

        public override void GenerateCode(StringBuilder code, TargetMember targetMember)
        {
            code.Append($"            public {ReturnType?.ToDisplayString() ?? "void"} {Name} {{");

            var callee = GetMemberCallee(targetMember);
            if (IsGet)
            {
                code.Append($" get => {callee}{GetShimCode(targetMember)};");
            }
            if (IsSet)
            {
                code.Append($" set => {callee} = value{GetUnshimCode(targetMember)};");
            }
            if (IsInit)
            {
                code.Append($" init => {callee} = value{GetUnshimCode(targetMember)};");
            }

            code.AppendLine(" }");
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

        if (symbol is IPropertySymbol ps)
        {
            Diag.WriteOutput($"//// {symbol.Name} {symbol.Kind} {ps.IsIndexer} {ps.Parameters.Length} {ps.Parameters[0].ToDisplayString()}");
        }

        // Build member
        return symbol switch
        {
            ISymbol { IsAbstract: false } => null,
            IEventSymbol eventSymbol => new ShimEventMember(shim, eventSymbol),
            IPropertySymbol { IsIndexer: true } property => new ShimIndexerMember(shim, property),
            IPropertySymbol property => new ShimPropertyMember(shim, property),
            // TODO: property.ExplicitInterfaceImplementations.Any()?
            IMethodSymbol { MethodKind: MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.ExplicitInterfaceImplementation or MethodKind.PropertyGet or MethodKind.PropertySet } => null,
            IMethodSymbol method => constructionType == null
                ? new ShimMethodMember(shim, method)
                : new ShimConstructorMember(shim, method, constructionType!),
            _ => throw new NotSupportedException($"Unhandled symbol type: {symbol.GetType().FullName}"),
        };
    }

    public IShimDefinition Definition { get; }
    public ISymbol Symbol { get; }
    public INamedTypeSymbol ContainingType => Symbol.ContainingType;
    public bool IsFactoryMember { get; }
    public string Name => Symbol.Name;
    public MemberType Type { get; }
    public virtual ITypeSymbol? ReturnType { get; }
    public string TargetName { get; }

    public ShimTarget? TargetType { get; set; }
    public (ITypeSymbol ImplementationType, string? ImplementationName, ProxyBehaviour Behaviour)? Proxy { get; }

    private ShimMember(IShimDefinition shim, ISymbol symbol, MemberType type)
    {
        Definition = shim;
        Symbol = symbol;
        IsFactoryMember = shim is ShimFactoryDefinition;
        Type = type;

        // Check for proxy attribute
        var proxyAttr = Symbol.GetAttribute<ShimProxyAttribute>();
        if (proxyAttr != null)
        {
            Proxy = ShimProxyAttribute.GetArguments(proxyAttr);
        }

        // Check for rename via attribute
        var attr = symbol.GetAttribute<ShimAttribute>();
        TargetName = (attr?.ConstructorArguments.Length) switch // Could be (string), (Type), or (Type, string)
        {
            1 => attr.ConstructorArguments[0].Type!.IsType<string>()
                ? attr.ConstructorArguments[0].Value?.ToString() : null,
            2 => attr.ConstructorArguments[1].Value?.ToString(),
            _ => null
        } ?? Name;
    }

    public abstract void GenerateCode(StringBuilder code, TargetMember targetMember);

    public string GetMemberCallee(TargetMember targetMember)
        => IsFactoryMember || targetMember.IsStatic
        ? $"{targetMember.ContainingType.ToFullName()}.{targetMember.Name}"
        : $"(({targetMember.ContainingType.ToFullName()})_inst).{targetMember.Name}";

    public string? GetShimCode(TargetMember target)
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
    public string GetUnshimCode(TargetMember target)
        => ReturnType?.IsMatch(target.ReturnType) == false
        ? $".Unshim<{target.ReturnType!.ToDisplayString()}>()"
        : string.Empty;

    public virtual bool IsMatch(IMember member)
    {
        return member.Name == TargetName;
    }

    public virtual void ResolveBindings(IList<IBinding> bindings, TargetMember targetMember, CodeErrorReporter errors, ShimResolver shimResolver)
    {
        // Register return shim, if needed
        if (targetMember!.IsShimmableReturnType(this, out var targetElement, out var shimElement))
        {
            if (targetElement != null && shimElement != null)
            {
                safeRegister(shimElement, targetElement);
            }
        }
        else if (ReturnType!.TypeKind == TypeKind.Interface)
        {
            // Optimistic
            safeRegister(ReturnType, targetMember.ReturnType!);
        }
        else
        {
            // Error no matching method
            Diag.WriteOutput($"//// No binding match: {targetMember.Target.FullTypeName}.{TargetName} for {Definition.FullTypeName}.{Name}");
            errors.NoMemberError(targetMember.ContainingType, Symbol);
            return;
        }

        if (targetMember.Target is ShimProxyTarget proxyTarget)
        {
            bindings.Add(new ShimMemberProxyBinding(this, proxyTarget.ShimTarget, targetMember));
        }
        else
        {
            bindings.Add(new ShimMemberBinding(this, targetMember));
        }

        void safeRegister(ITypeSymbol shimType, ITypeSymbol targetType)
        {
            var shim = shimResolver.GetOrCreate(shimType, false);
            if (shim is InstanceShimDefinition def)
            {
                def.AddTarget(targetType);
            }
            else
            {
                // TODO: Error for bad use of factory shim as instance shim
                errors.CodeGenError(new Exception($"Fetching factory shim as instance shim: {targetMember.ContainingType}, {Symbol}"));
            }
        }
    }
}
