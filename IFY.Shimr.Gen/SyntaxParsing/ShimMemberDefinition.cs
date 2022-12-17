using Microsoft.CodeAnalysis;
using System.Diagnostics;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

internal class ShimMemberDefinition
{
    public ISymbol Symbol { get; }

    public INamedTypeSymbol ShimType { get; }
    public string ShimTypeFullName { get; }

    public SymbolKind Kind { get; }
    public string Name { get; }
    public string SignatureName { get; }
    public string? TargetName { get; private set; }
    public TypeDef? ReturnType { get; private set; }
    public TypeDef? TargetReturnType { get; set; }
    public bool IsReturnShim { get; set; }
    public Dictionary<string, MethodParameterDefinition> Parameters { get; } = new();
    public INamedTypeSymbol? ParentType { get; }

    public bool CanRead { get; }
    public bool CanWrite { get; }
    public TypeDef? IndexType { get; }
    public bool UsePropertyMethods { get; private set; }

    public TypeDef? StaticType { get; set; }
    public bool IsStatic { get; private set; }
    public bool IsConstructor { get; private set; }

    public string[]? GenericContraints { get; }

    public class ProxyInfo
    {
        public bool IsOverride { get; }
        public bool? IsExtensionMethod { get; } // true = "this", false == "_obj" as first arg
        public INamedTypeSymbol Type { get; }
        public string Name { get; }

        public bool? IsExtensionSetMethod { get; }

        public ProxyInfo(AttributeData proxyAttr, ShimMemberDefinition member, INamedTypeSymbol type, TypeDef targetType)
        {
            Name = member.Name;
            Type = type;

            if (proxyAttr.TryGetAttributeConstructorValue("implementationName", out var proxyMemberNameArg) && proxyMemberNameArg != null)
            {
                Name = proxyMemberNameArg.ToString();
            }

            // Find target member
            if (member.Kind == SymbolKind.Method)
            {
                var proxyMethod = findProxyMethod(Name, Type, member.ShimType, new TypeDef[member.Parameters.Count], out var isExtensionMethod);
                IsExtensionMethod = isExtensionMethod;
                if (proxyMethod == null)
                {
                    member.Symbol.ReportProxyMemberMissing(member.ShimTypeFullName, Name, Type.FullName());
                    return;
                }
            }
            else
            {
                var proxyProps = Type.GetMembers()
                    .Where(m => m.Name == Name && m.Kind == SymbolKind.Property && m.IsStatic)
                    .Cast<IPropertySymbol>()
                    .ToArray();

                // Properties can also override methods
                // TODO: this should be in definition for use in non-proxy properties
                if (!proxyProps.Any())
                {
                    IMethodSymbol? getMethod = null, setMethod = null;
                    if (member.CanRead)
                    {
                        getMethod = findProxyMethod("get_" + Name, type, member.ShimType, Array.Empty<TypeDef>(), out var isExtGetMethod);
                        IsExtensionMethod = isExtGetMethod;
                    }
                    if (member.CanWrite)
                    {
                        setMethod = findProxyMethod("set_" + Name, type, member.ShimType, new[] { member.ReturnType }, out var isExtSetMethod);
                        IsExtensionSetMethod = isExtSetMethod;
                    }

                    member.UsePropertyMethods = member.CanRead == (getMethod != null) && member.CanWrite == (setMethod != null);
                    if (!member.UsePropertyMethods)
                    {
                        member.Symbol.ReportProxyMemberMissing(member.ShimTypeFullName, Name, Type.FullName());
                        return;
                    }
                }
            }

            // Find base type member
            IsOverride = member.Kind == SymbolKind.Method
                ? GetMatchingMethods(targetType.Symbol, member.TargetName ?? member.Name, member.ReturnType?.Symbol, ((IMethodSymbol)member.Symbol).Parameters, false).Any()
                : GetMatchingProperties(targetType.Symbol, member.TargetName ?? member.Name, ((IPropertySymbol)member.Symbol).Type, false, ((IPropertySymbol)member.Symbol).CanRead(), ((IPropertySymbol)member.Symbol).CanWrite()).Any();
            if (proxyAttr.TryGetAttributeConstructorValue("behaviour", out var proxyBehaviour)
                && proxyBehaviour != null)
            {
                if ((int)proxyBehaviour == (int)ProxyBehaviour.Add && IsOverride)
                {
                    // Cannot use Add if base type contains method
                    member.Symbol.ReportProxyAddExisting(member.ShimTypeFullName, member.TargetName ?? member.Name, targetType.FullName);
                }
                else if ((int)proxyBehaviour == (int)ProxyBehaviour.Override && !IsOverride)
                {
                    // Can only use Override if existing base method
                    member.Symbol.ReportProxyOverrideMissing(member.ShimTypeFullName, member.TargetName ?? member.Name, targetType.FullName);
                }
            }
        }

        private static IMethodSymbol? findProxyMethod(string name, INamedTypeSymbol type, INamedTypeSymbol memberParent, IEnumerable<TypeDef?> parameters, out bool? isExtensionMethod)
        {
            isExtensionMethod = null;
            var proxyMethods = type.GetMembers()
                .Where(m => m.Name == name && m.Kind == SymbolKind.Method && m.IsStatic)
                // TODO: match parameters
                .Cast<IMethodSymbol>()
                .ToArray();

            foreach (var method in proxyMethods)
            {
                // Find first method that properly extends
                if (method.Parameters.Length == parameters.Count() + 1)
                {
                    var arg0Type = proxyMethods[0].Parameters[0].Type;
                    isExtensionMethod = IsAssignableTo(arg0Type, memberParent)
                        ? true
                        : IsAssignableTo(arg0Type, type)
                        ? false
                        : null;
                    if (isExtensionMethod != null)
                    {
                        return method;
                    }
                }
            }
            return proxyMethods.FirstOrDefault(m => m.Parameters.Length == parameters.Count());
        }
    }
    public ProxyInfo? Proxy { get; private set; }

    public ShimMemberDefinition(IEventSymbol ev)
    {
        // Basics
        Symbol = ev;
        ShimType = ev.ContainingType;
        ShimTypeFullName = ev.ContainingType.FullName();
        Kind = SymbolKind.Event;
        Name = ev.Name;
        SignatureName = Name;
        ReturnType = new((INamedTypeSymbol)ev.Type);
    }

    public ShimMemberDefinition(IPropertySymbol property, TypeDef targetType)
    {
        // Basics
        Symbol = property;
        ShimType = property.ContainingType;
        ShimTypeFullName = property.ContainingType.FullName();
        Kind = SymbolKind.Property;
        Name = property.Name.Trim('[', ']');
        SignatureName = Name;
        CanRead = property.GetMethod != null;
        CanWrite = property.SetMethod != null;
        IndexType = property.IsIndexer
            ? new((INamedTypeSymbol)property.Parameters[0].Type) : null;
        ReturnType = new(property.Type);

        // TODO: Property may also match to methods

        // Attributes
        parseAttributes(property, targetType);
    }

    public ShimMemberDefinition(IMethodSymbol method, TypeDef targetType)
    {
        // Basics
        Symbol = method;
        ShimType = method.ContainingType;
        ShimTypeFullName = method.ContainingType.FullName();
        Kind = SymbolKind.Method;
        Name = method.Name;
        if (method.TryGetReturnType(out var returnType))
        {
            ReturnType = returnType;
        }

        // Type Parameters
        if (method.TypeParameters.Any())
        {
            var constraints = new List<string>();
            for (var i = 0; i < method.TypeParameters.Length; ++i)
            {
                Name += (i == 0 ? "<" : ", ") + method.TypeParameters[i].Name;

                var constraint = method.TypeParameters[i].TypeConstraintString();
                if (constraint?.Length > 0)
                {
                    constraints.Add(constraint);
                }
            }
            Name += ">";
            if (constraints.Any())
            {
                GenericContraints = constraints.ToArray();
            }
        }

        var methods = GetMatchingMethods(targetType.Symbol, Name, ReturnType?.Symbol, method.Parameters, false);
        if (!methods.Any())
        {
            // TODO: warning
        }
        else
        {
            ParentType = methods.First().ContainingType;
        }

        // Parameters
        foreach (var parameter in method.Parameters)
        {
            Parameters.Add(parameter.Name, new MethodParameterDefinition(parameter));
        }

        SignatureName = Name + (method.TypeArguments.Any() ? "`" + method.TypeArguments.Length : null)
            + "(" + string.Join(",", method.Parameters.Select(p => p.Type.Name)) + ")";

        // Attributes
        parseAttributes(method, targetType);
    }

    private void parseAttributes(ISymbol symbol, TypeDef targetType)
    {
        // ShimAttribute
        var shimAttr = symbol.GetAttribute<ShimAttribute>();
        if (shimAttr != null)
        {
            // TODO: support definitionType arg
            if (shimAttr.TryGetAttributeConstructorValue("name", out var targetName))
            {
                TargetName = targetName?.ToString();
            }
        }

        // ShimProxyAttribute
        var proxyAttr = symbol.GetAttribute<ShimProxyAttribute>();
        if (proxyAttr != null
            // implementationType is required
            && proxyAttr.TryGetAttributeConstructorValue("implementationType", out var proxyTypeArg)
            && proxyTypeArg != null)
        {
            Proxy = new(proxyAttr, this, (INamedTypeSymbol)proxyTypeArg, targetType);
            StaticType = new(Proxy.Type);
        }

        // ConstructorShimAttribute
        var constrAttr = symbol.GetAttribute<ConstructorShimAttribute>();
        if (constrAttr != null)
        {
            if (proxyAttr != null)
            {
                symbol.ReportConflictingAttributes(ShimTypeFullName, Name, typeof(ShimProxyAttribute).FullName, typeof(StaticShimAttribute).FullName);
            }
            else
            {
                IsConstructor = true;
                IsStatic = true;
                constrAttr.TryGetAttributeConstructorValue("targetType", out var constrTargetType);
                StaticType = constrTargetType is INamedTypeSymbol type ? new(type) : null;
                TargetReturnType = constrTargetType is INamedTypeSymbol type2 ? new(type2) : null;
                IsReturnShim = true;
            }
        }
        else
        {
            // StaticShimAttribute
            var staticAttr = symbol.GetAttribute<StaticShimAttribute>();
            if (staticAttr?.TryGetAttributeConstructorValue("targetType", out var staticTargetType) == true)
            {
                if (proxyAttr != null)
                {
                    symbol.ReportConflictingAttributes(ShimTypeFullName, Name, typeof(ShimProxyAttribute).FullName, typeof(StaticShimAttribute).FullName);
                }
                else
                {
                    StaticType = staticTargetType is INamedTypeSymbol type ? new(type) : null;
                    IsStatic = true;
                }
            }
        }
    }

    private static bool IsAssignableTo(ITypeSymbol symbol, ITypeSymbol type)
    {
        // Any interface match
        if (symbol.AllInterfaces.Select(i => i.FullName()).Union(type.AllInterfaces.Select(i => i.FullName())).Any())
        {
            return true;
        }
        // Any class in hierarchy
        var symbolFullName = symbol.TryFullName();
        while (type != null)
        {
            if (symbolFullName == type.TryFullName())
            {
                return true;
            }
            type = type.BaseType!;
        }
        return false;
    }

    private static IMethodSymbol[] GetMatchingMethods(ITypeSymbol type, string name, ITypeSymbol? returnType, IEnumerable<IParameterSymbol> parameters, bool isStatic)
    {
        return type.GetAllMembers()
            .Where(m => m.Kind == SymbolKind.Method && m.Name == name && m.IsStatic == isStatic)
            .OfType<IMethodSymbol>()
            .Where(m => (returnType == null && m.ReturnsVoid) || m.ReturnType.TryFullName() == returnType?.TryFullName())
            .Where(allParametersMatch)
            .ToArray();
        bool allParametersMatch(IMethodSymbol method)
        {
            if (method.Parameters.Length != parameters.Count())
            {
                return false;
            }
            for (var i = 0; i < method.Parameters.Length; ++i)
            {
                if (method.Parameters[i].Type.TryFullName() != parameters.ElementAt(i).Type.TryFullName())
                {
                    return false;
                }
            }
            return true;
        }
    }
    private static IPropertySymbol[] GetMatchingProperties(ITypeSymbol type, string name, ITypeSymbol propertyType, bool isStatic, bool canRead, bool canWrite)
    {
        return type.GetMembers()
            .Where(m => m.Kind == SymbolKind.Property && m.Name == name && m.IsStatic == isStatic)
            .OfType<IPropertySymbol>()
            .Where(p => p.Type.TryFullName() == propertyType.TryFullName()
                && p.CanRead() == canRead && p.CanWrite() == canWrite)
            .ToArray();
    }
}
