using Microsoft.CodeAnalysis;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen.SyntaxParsing;

/// <summary>
/// Represents a type within the code.
/// </summary>
internal class TypeDef
{
    public ISymbol Symbol { get; }

    public TypeKind Kind { get; }

    public bool IsNullable { get; }
    public bool IsValueType { get; }
    public string Name { get; }
    public string Namespace { get; }

    /// <summary>
    /// The full referencable name of the type.
    /// Include generic arguments, "[]", "?", etc.
    /// </summary>
    public string FullName { get; }
    /// <summary>
    /// Same as <see cref="FullName"/>, but generic args are unspecified ("&lt;&gt;")
    /// </summary>
    public string FullGenericName { get; }

    public TypeDef? ArrayElementType { get; } // If not null, array

    public bool IsGeneric => GenericArgs.Length > 0;
    public ITypeSymbol[] GenericArgs { get; set; } = Array.Empty<ITypeSymbol>(); // Could be resolved types or template args
    public string? GenericArgList { get; }

    public INamedTypeSymbol[] AllInterfaces { get; } = Array.Empty<INamedTypeSymbol>();

    public TypeDef(INamedTypeSymbol type)
    {
        Symbol = type;
        Kind = type.TypeKind;
        IsNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
        IsValueType = type.IsValueType;
        GenericArgs = type.TypeArguments.ToArray();
        GenericArgList = IsGeneric ? "<" + string.Join(",", GenericArgs.Select(a => a.Name)) + ">" : null;
        Name = type.GetName();
        Namespace = type.FullNamespace();
        FullName = type.FullName(); // TODO
        FullGenericName = type.FullName(true) + (IsGeneric ? "<" + new string(',', GenericArgs.Length - 1) + ">" : null);
        AllInterfaces = type.AllInterfaces.ToArray();
    }

    public TypeDef(ITypeParameterSymbol typepar)
    {
        Symbol = typepar;
        Kind = TypeKind.TypeParameter;
        IsNullable = typepar.NullableAnnotation == NullableAnnotation.Annotated;
        Name = typepar.Name;
        Namespace = string.Empty;
        FullName = Name;
        FullGenericName = FullName;
    }

    public TypeDef(IArrayTypeSymbol arr)
    {
        Symbol = arr;
        Kind = TypeKind.Array;
        IsNullable = arr.NullableAnnotation == NullableAnnotation.Annotated;
        ArrayElementType = new TypeDef((INamedTypeSymbol)arr.ElementType);
        Name = ArrayElementType.Name;
        Namespace = ArrayElementType.Namespace;
        FullName = ArrayElementType.FullName + "[]";
        FullGenericName = FullName;
    }

    public ISymbol[] GetMembers()
    {
        if (ArrayElementType != null)
        {
            return ArrayElementType!.GetMembers();
        }
        if (Symbol is ITypeSymbol type)
        {
            IEnumerable<ISymbol> members = type.GetMembers();
            if (type.TypeKind == TypeKind.Interface)
            {
                members = members.Union(AllInterfaces.SelectMany(i => i.GetMembers()));
            }
            return members.ToArray();
        }
        return Array.Empty<ISymbol>();
    }

    public override bool Equals(object obj) => obj is TypeDef d && d.FullName == FullName;
    public override int GetHashCode() => FullName.GetHashCode();
}
