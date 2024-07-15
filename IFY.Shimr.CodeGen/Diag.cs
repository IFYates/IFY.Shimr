namespace IFY.Shimr.CodeGen;

/// <summary>
/// Helpers for diagnosing issues.
/// </summary>
internal static class Diag
{
    private const string OUTPUT_FILE = "F:\\Dev\\IFY.Shimr\\Sourcegen.cs";

    public static bool IsEnabled { get; set; } = true;

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
        if (IsEnabled)
        {
            try
            {
                using FileStream fs = new(OUTPUT_FILE, append ? FileMode.Append : FileMode.Create);
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