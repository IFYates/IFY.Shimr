namespace IFY.Shimr.SourceGen;

/// <summary>
/// Helpers for diagnosing issues.
/// </summary>
internal static class Diag
{
    private static readonly StringBuilder _text = new();
    private const string OUTPUT_FILE = "obj\\Diag.Output.cs";

    public static bool IsOutputEnabled { get; set; } = true;

    private static string? _outputPath;
    public static string? OutputPath
    {
        get => _outputPath;
        set
        {
            _outputPath = Path.Combine(value, OUTPUT_FILE);
            if (_text.Length > 0)
            {
                if (_text[_text.Length - 1] == '\n') { _text.Remove(_text.Length - 1, 1); }
                if (_text[_text.Length - 1] == '\r') { _text.Remove(_text.Length - 1, 1); }
                WriteOutput(_text.ToString(), false);
                _text.Clear();
            }
        }
    }

    private static bool _hasLaunched = false;

    public static void Debug()
    {
        if (!_hasLaunched)
        {
            _hasLaunched = true;
            System.Diagnostics.Debugger.Launch();
        }
        else
        {
            System.Diagnostics.Debugger.Break();
        }
    }

    public static void WriteOutput(string text, bool append = true)
    {
#if DEBUG
        if (IsOutputEnabled)
        {
            if (OutputPath == null)
            {
                _text.AppendLine(text);
                return;
            }

            try
            {
                using FileStream fs = new(_outputPath, append ? FileMode.Append : FileMode.Create);
                using StreamWriter sw = new(fs);
                sw.WriteLine(text);
            }
            catch
            {
            }
        }
#endif
    }
}