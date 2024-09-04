using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen.Models.Members;

/// <summary>
/// A code member.
/// </summary>
internal interface IMember
{
    ISymbol Symbol { get; }
    INamedTypeSymbol ContainingType { get; }
    string Name { get; }
    MemberType Type { get; }
    ITypeSymbol? ReturnType { get; }
}
