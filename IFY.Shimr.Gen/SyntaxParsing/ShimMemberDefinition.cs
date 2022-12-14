using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

internal class ShimMemberDefinition
{
    public INamedTypeSymbol ParentType { get; }
    public string ParentTypeFullName { get; }

    public SymbolKind Kind { get; }
    public string Name { get; }
    public string SignatureName { get; }
    public string? TargetName { get; private set; }
    public TypeDef? ReturnType { get; private set; }
    public TypeDef? TargetReturnType { get; set; }
    public bool IsReturnShim { get; set; }

    public bool CanRead { get; }
    public bool CanWrite { get; }

    public TypeDef? StaticType { get; set; }
    public bool IsStatic { get; private set; }
    public bool IsConstructor { get; private set; }
    public bool? IsExtensionProxy { get; private set; } // true = "this", false == "_obj" as first arg
    public bool IsProxyOverride { get; private set; }

    public string[]? GenericContraints { get; }

    public Dictionary<string, MethodParameterDefinition> Parameters { get; } = new();

    public ShimMemberDefinition(IEventSymbol ev)
    {
        // Basics
        ParentType = ev.ContainingType;
        ParentTypeFullName = ev.ContainingType.FullName();
        Kind = SymbolKind.Event;
        Name = ev.Name;
        SignatureName = Name;
        ReturnType = new((INamedTypeSymbol)ev.Type);
    }

    public ShimMemberDefinition(IPropertySymbol property, TypeDef targetType)
    {
        // Basics
        ParentType = property.ContainingType;
        ParentTypeFullName = property.ContainingType.FullName();
        Kind = SymbolKind.Property;
        Name = property.Name;
        SignatureName = Name;
        CanRead = property.GetMethod != null;
        CanWrite = property.SetMethod != null;
        if (property.TryGetReturnType(out var returnType))
        {
            ReturnType = returnType;
        }

        // Attributes
        parseAttributes(property, targetType);
    }

    public ShimMemberDefinition(IMethodSymbol method, TypeDef targetType)
    {
        // Basics
        ParentType = method.ContainingType;
        ParentTypeFullName = method.ContainingType.FullName();
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
        // ShimProxyAttribute
        var proxyAttr = symbol.GetAttribute<ShimProxyAttribute>();
        if (proxyAttr != null
            // implementationType is required
            && proxyAttr.TryGetAttributeConstructorValue("implementationType", out var proxyTypeArg)
            && proxyTypeArg != null)
        {
            StaticType = new((INamedTypeSymbol)proxyTypeArg);

            TargetName = Name;
            if (proxyAttr.TryGetAttributeConstructorValue("implementationName", out var proxyMemberNameArg) && proxyMemberNameArg != null)
            {
                TargetName = proxyMemberNameArg.ToString();
            }

            // Find target member
            if (Kind == SymbolKind.Method)
            {
                var proxyMethods = StaticType.GetMembers()
                    .Where(m => m.Name == TargetName && m.Kind == Kind)
                    // TODO: match parameters
                    .Cast<IMethodSymbol>()
                    .ToArray();

                IMethodSymbol? proxyMethod = null;
                foreach (var method in proxyMethods)
                {
                    // Find first method that properly extends
                    if (method.Parameters.Length == Parameters.Count + 1)
                    {
                        var arg0Type = proxyMethods[0].Parameters[0].Type;
                        IsExtensionProxy = IsAssignableTo(arg0Type, ParentType)
                            ? true
                            : IsAssignableTo(arg0Type, StaticType.Symbol)
                            ? false
                            : null;
                        if (IsExtensionProxy != null)
                        {
                            proxyMethod = method;
                            break;
                        }
                    }
                }
                proxyMethod ??= proxyMethods
                        .FirstOrDefault(m => m.Parameters.Length == Parameters.Count);
                if (proxyMethod == null)
                {
                    symbol.ReportProxyMemberMissing(ParentTypeFullName, Name, StaticType.FullName);
                    return;
                }
            }
            else
            {
                // TODO: how does property work?
                throw new NotImplementedException();
            }

            // Find base type member
            IsProxyOverride = Kind == SymbolKind.Method
                ? GetMatchingMethods(targetType.Symbol, Name, ReturnType?.Symbol, ((IMethodSymbol)symbol).Parameters, false).Any()
                : GetMatchingProperties(targetType.Symbol, Name, ((IPropertySymbol)symbol).Type, false).Any();
            if (proxyAttr.TryGetAttributeConstructorValue("behaviour", out var proxyBehaviour)
                && proxyBehaviour != null)
            {
                if ((int)proxyBehaviour == (int)ProxyBehaviour.Add && IsProxyOverride)
                {
                    // Cannot use Add if base type contains method
                    symbol.ReportProxyAddExisting(ParentTypeFullName, Name, targetType.FullName);
                }
                else if ((int)proxyBehaviour == (int)ProxyBehaviour.Override && !IsProxyOverride)
                {
                    // Can only use Override if existing base method
                    symbol.ReportProxyOverrideMissing(ParentTypeFullName, Name, targetType.FullName);
                }
            }
        }

        // ShimAttribute
        var shimAttr = symbol.GetAttribute<ShimAttribute>();
        if (shimAttr != null)
        {
            if (proxyAttr != null)
            {
                symbol.ReportConflictingAttributes(ParentTypeFullName, Name, typeof(ShimProxyAttribute).FullName, typeof(ShimAttribute).FullName);
            }
            else
            {
                // TODO: support definitionType arg
                if (shimAttr.TryGetAttributeConstructorValue("name", out var targetName))
                {
                    TargetName = targetName?.ToString();
                }
            }
        }

        // StaticShimAttribute
        var staticAttr = symbol.GetAttribute<StaticShimAttribute>();
        if (staticAttr?.TryGetAttributeConstructorValue("targetType", out var staticTargetType) == true)
        {
            StaticType = staticTargetType is INamedTypeSymbol type ? new(type) : null;
            IsStatic = true;
        }

        // ConstructorShimAttribute
        var constrAttr = symbol.GetAttribute<ConstructorShimAttribute>();
        if (constrAttr != null)
        {
            IsConstructor = true;
            IsStatic = true;
            constrAttr.TryGetAttributeConstructorValue("targetType", out var constrTargetType);
            StaticType = constrTargetType is INamedTypeSymbol type ? new(type) : null;
            TargetReturnType = constrTargetType is INamedTypeSymbol type2 ? new(type2) : null;
            IsReturnShim = true;
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

    private static IMethodSymbol[] GetMatchingMethods(ITypeSymbol type, string name, ITypeSymbol? returnType, ImmutableArray<IParameterSymbol> parameters, bool isStatic)
    {
        return type.GetMembers()
            .Where(m => m.Kind == SymbolKind.Method && m.Name == name && m.IsStatic == isStatic)
            .OfType<IMethodSymbol>()
            .Where(m => (returnType == null && m.ReturnsVoid) || m.ReturnType.TryFullName() == returnType?.TryFullName())
            .Where(allParametersMatch)
            .ToArray();
        bool allParametersMatch(IMethodSymbol method)
        {
            if (method.Parameters.Length != parameters.Length)
            {
                return false;
            }
            for (var i = 0; i < method.Parameters.Length; ++i)
            {
                if (method.Parameters[i].Type.TryFullName() != parameters[i].Type.TryFullName())
                {
                    return false;
                }
            }
            return true;
        }
    }
    private static IPropertySymbol[] GetMatchingProperties(ITypeSymbol type, string name, ITypeSymbol propertyType, bool isStatic)
    {
        return type.GetMembers()
            .Where(m => m.Kind == SymbolKind.Property && m.Name == name && m.IsStatic == isStatic)
            .OfType<IPropertySymbol>()
            .Where(m => m.Type.TryFullName() == propertyType.TryFullName())
            .ToArray();
    }
}
