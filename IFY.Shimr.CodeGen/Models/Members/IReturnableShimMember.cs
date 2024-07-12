using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal interface IReturnableShimMember : IShimMember
{
    ITypeSymbol ReturnType { get; }
    string ReturnTypeName { get; }

    ITypeSymbol GetUnderlyingMemberReturn(ITypeSymbol underlyingType);
}