namespace IFY.Shimr.CodeGen;

internal interface ICodeWriter
{
    bool HasNullableAttributes { get; }
    bool HasStackTraceHiddenAttribute { get; }

    void Append(string value);
    void AppendLine(string value);
    void AppendFormat(string format, params object?[] args);

    /// <summary>
    /// Write the current code buffer to a source file.
    /// </summary>
    /// <param name="name">The 'hintName' of the source file.</param>
    void WriteSource(string name);
}