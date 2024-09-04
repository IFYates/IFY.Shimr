namespace IFY.Shimr.SourceGen.Models.Members;

/// <summary>
/// A type member with parameters.
/// </summary>
internal interface IParameterisedMember : IMember
{
    MemberParameter[] Parameters { get; }
}
