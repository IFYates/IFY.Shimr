namespace IFY.Shimr.CodeGen;

internal interface ICodeWriter
{
    bool HasNullableAttributes { get; }
    void AddSource(string name, string code);
}