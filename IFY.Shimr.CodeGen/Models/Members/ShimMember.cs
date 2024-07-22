using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

/// <summary>
/// A shim member.
/// </summary>
internal abstract class ShimMember : IMember
{
    public sealed class ShimConstructorMember(IShimDefinition shim, IMethodSymbol symbol)
        : ShimMethodMember(shim, symbol, MemberType.Constructor), IParameterisedMember
    {
        protected override void AddInvocationCode(StringBuilder code, TargetMember targetMember, string? defTypeArgs)
        {
            code.Append($"new {GetMemberCallee(targetMember)}");
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

            var callee = $"{GetMemberCallee(targetMember)}[{IndexerArgName}]";
            if (IsGet)
            {
                code.Append($" get => {callee}{GetShimCode(targetMember)};");
            }
            if (IsSet)
            {
                code.Append($" set => {callee} = {GetUnshimCode("value", targetMember)};");
            }
            if (IsInit)
            {
                code.Append($" init => {callee} = {GetUnshimCode("value", targetMember)};");
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
            // Check if explicit implementation
            var name = IsExplicit ? $"{ContainingType.ToFullName()}.{Name}" : Name;
            code.Append("            ");
            if (!IsExplicit)
            {
                code.Append("public ");
            }

            if (((IParameterisedMember)targetMember).Parameters.Length == 0
                && Name is nameof(ToString) or nameof(GetHashCode))
            {
                code.Append("override ");
            }

            string? defTypeArgs = null, whereClause = null;
            if (symbol.IsGenericMethod)
            {
                defTypeArgs = symbol.TypeParameters.ToTypeParameterList();
                whereClause = symbol.TypeParameters.ToWhereClause();
            }

            code.Append($"{ReturnType?.ToDisplayString() ?? "void"} {name}{defTypeArgs}(")
                .Append(string.Join(", ", Parameters.Select(p => p.ToString())))
                .Append($"){whereClause} => ");
            AddInvocationCode(code, targetMember, defTypeArgs);
            code.Append("(")
                .Append(string.Join(", ", Parameters.Select(p => p.GetTargetArgumentCode())))
                .Append($")")
                .Append(GetShimCode(targetMember))
                .AppendLine("; // " + IsExplicit);
        }
        protected virtual void AddInvocationCode(StringBuilder code, TargetMember targetMember, string? defTypeArgs)
        {
            code.Append($"{GetMemberCallee(targetMember)}.{targetMember.Name}{defTypeArgs}");
        }

        public bool FirstParameterIsInstance(IParameterisedMember targetMethod)
        {
            return targetMethod.Parameters.Length == Parameters.Length + 1
                && targetMethod.Parameters[0].Type.IsMatchable(ContainingType);
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
            // Check if explicit
            if (IsExplicit)
            {
                code.Append($"            {ReturnType?.ToDisplayString() ?? "void"} {Symbol.ContainingType.ToFullName()}.{Name} {{");
            }
            else
            {
                code.Append($"            public {ReturnType?.ToDisplayString() ?? "void"} {Name} {{");
            }

            var callee = $"{GetMemberCallee(targetMember)}.{targetMember.Name}";
            if (IsGet)
            {
                code.Append($" get => {callee}{GetShimCode(targetMember)};");
            }
            if (IsSet)
            {
                code.Append($" set => {callee} = {GetUnshimCode("value", targetMember)};");
            }
            if (IsInit)
            {
                code.Append($" init => {callee} = {GetUnshimCode("value", targetMember)};");
            }

            code.AppendLine(" }");
        }
    }

    public static ShimMember? Parse(ISymbol symbol, IShimDefinition shim, IEnumerable<ISymbol> shimMembers)
    {
        // Get constructor attribute
        var isConstructor = shim is ShimFactoryDefinition
            && symbol.GetAttribute<ConstructorShimAttribute>() != null;

        // Build member
        ShimMember? member = symbol switch
        {
            ISymbol { IsAbstract: false } => null,
            IEventSymbol eventSymbol => new ShimEventMember(shim, eventSymbol),
            IPropertySymbol { IsIndexer: true } property => new ShimIndexerMember(shim, property),
            IPropertySymbol property => new ShimPropertyMember(shim, property),
            // TODO: property.ExplicitInterfaceImplementations.Any()?
            IMethodSymbol { MethodKind: MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.ExplicitInterfaceImplementation or MethodKind.PropertyGet or MethodKind.PropertySet } => null,
            IMethodSymbol method => !isConstructor
                ? new ShimMethodMember(shim, method)
                : new ShimConstructorMember(shim, method),
            _ => throw new NotSupportedException($"Unhandled symbol type: {symbol.GetType().FullName}"),
        };

        // Check if explicit implementation
        if (member != null)
        {
            var key = symbol.GetMemberUniqueName(false);
            var similar = shimMembers.Where(m => !ReferenceEquals(m, symbol) && m.GetMemberUniqueName(false) == key).ToArray();
            if (similar.Any(m => m.ContainingType.AllInterfaces.Any(symbol.ContainingType.IsMatch)))
            {
                member.IsExplicit = true;
            }
        }

        return member;
    }

    public IShimDefinition Definition { get; }
    public ISymbol Symbol { get; }
    public INamedTypeSymbol ContainingType => Symbol.ContainingType;
    public bool IsFactoryMember { get; }
    public string Name => Symbol.Name;
    public MemberType Type { get; }
    public virtual ITypeSymbol? ReturnType { get; }
    public string TargetName { get; }
    public bool IsExplicit { get; private set; }

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
        ? targetMember.ContainingType.ToFullName()
        : $"(({targetMember.ContainingType.ToFullName()})_inst)";

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
    public string GetUnshimCode(string name, TargetMember target)
        => ReturnType?.IsMatch(target.ReturnType) == false
        ? $"({target.ReturnType!.ToDisplayString()})((IShim){name}).Unshim()"
        : name;

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
        else if (ReturnType?.TypeKind == TypeKind.Interface)
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

        bindings.Add(targetMember.Target.GetBinding(this, targetMember));

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
