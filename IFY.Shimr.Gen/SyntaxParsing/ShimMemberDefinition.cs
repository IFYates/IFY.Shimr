using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

internal class ShimMemberDefinition
{
    public ISymbol Symbol { get; }

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

    public string[]? GenericContraints { get; }

    public class ProxyInfo
    {
        public bool IsOverride { get; }
        public bool? IsExtensionMethod { get; } // true = "this", false == "_obj" as first arg
        public INamedTypeSymbol Type { get; }
        public string Name { get; }

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
                var proxyMethods = Type.GetMembers()
                    .Where(m => m.Name == Name && m.Kind == member.Kind)
                    // TODO: match parameters
                    .Cast<IMethodSymbol>()
                    .ToArray();

                IMethodSymbol? proxyMethod = null;
                foreach (var method in proxyMethods)
                {
                    // Find first method that properly extends
                    if (method.Parameters.Length == member.Parameters.Count + 1)
                    {
                        var arg0Type = proxyMethods[0].Parameters[0].Type;
                        IsExtensionMethod = IsAssignableTo(arg0Type, member.ParentType)
                            ? true
                            : IsAssignableTo(arg0Type, Type)
                            ? false
                            : null;
                        if (IsExtensionMethod != null)
                        {
                            proxyMethod = method;
                            break;
                        }
                    }
                }
                proxyMethod ??= proxyMethods
                        .FirstOrDefault(m => m.Parameters.Length == member.Parameters.Count);
                if (proxyMethod == null)
                {
                    member.Symbol.ReportProxyMemberMissing(member.ParentTypeFullName, Name, Type.FullName());
                    return;
                }
            }
            else
            {
                // TODO: how does property work?
                throw new NotImplementedException();
            }

            // Find base type member
            IsOverride = member.Kind == SymbolKind.Method
                ? GetMatchingMethods(targetType.Symbol, member.Name, member.ReturnType?.Symbol, ((IMethodSymbol)member.Symbol).Parameters, false).Any()
                : GetMatchingProperties(targetType.Symbol, member.Name, ((IPropertySymbol)member.Symbol).Type, false).Any();
            if (proxyAttr.TryGetAttributeConstructorValue("behaviour", out var proxyBehaviour)
                && proxyBehaviour != null)
            {
                if ((int)proxyBehaviour == (int)ProxyBehaviour.Add && IsOverride)
                {
                    // Cannot use Add if base type contains method
                    member.Symbol.ReportProxyAddExisting(member.ParentTypeFullName, member.Name, targetType.FullName);
                }
                else if ((int)proxyBehaviour == (int)ProxyBehaviour.Override && !IsOverride)
                {
                    // Can only use Override if existing base method
                    member.Symbol.ReportProxyOverrideMissing(member.ParentTypeFullName, member.Name, targetType.FullName);
                }
            }
        }
    }
    public ProxyInfo? Proxy { get; private set; }

    public Dictionary<string, MethodParameterDefinition> Parameters { get; } = new();

    public ShimMemberDefinition(IEventSymbol ev)
    {
        // Basics
        Symbol = ev;
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
        Symbol = property;
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
        Symbol = method;
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
            Proxy = new(proxyAttr, this, (INamedTypeSymbol)proxyTypeArg, targetType);
            StaticType = new(Proxy.Type);
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

        // ConstructorShimAttribute
        var constrAttr = symbol.GetAttribute<ConstructorShimAttribute>();
        if (constrAttr != null)
        {
            if (proxyAttr != null)
            {
                symbol.ReportConflictingAttributes(ParentTypeFullName, Name, typeof(ShimProxyAttribute).FullName, typeof(StaticShimAttribute).FullName);
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
                    symbol.ReportConflictingAttributes(ParentTypeFullName, Name, typeof(ShimProxyAttribute).FullName, typeof(StaticShimAttribute).FullName);
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
