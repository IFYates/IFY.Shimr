using Microsoft.CodeAnalysis.CSharp;

namespace IFY.Shimr.CodeGen;

internal interface ICodeWriter
{
    LanguageVersion CSLangver { get; }
    void AddSource(string name, StringBuilder code);
}