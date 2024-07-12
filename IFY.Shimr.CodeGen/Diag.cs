namespace IFY.Shimr.CodeGen;

/// <summary>
/// Helpers for diagnosing issues.
/// </summary>
internal static class Diag
{
    private const string OUTPUT_FILE = "F:\\Dev\\IFY.Shimr\\Sourcegen.cs";
    public static void WriteOutput(string text, bool append = true)
    {
#if DEBUG
        try
        {
            using FileStream fs = new(OUTPUT_FILE, append ? FileMode.Append : FileMode.Create);
            using StreamWriter sw = new(fs);
            sw.WriteLine(text);
        }
        catch
        {
        }
#endif
    }
}

// TODO: Instance class that raises diagnostic errors against current context