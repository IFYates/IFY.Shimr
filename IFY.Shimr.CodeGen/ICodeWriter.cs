namespace IFY.Shimr.CodeGen;

internal interface ICodeWriter
{
    bool HasNullableAttributes { get; }
    bool HasStackTraceHiddenAttribute { get; }
    void AddSource(string name, string code);
}