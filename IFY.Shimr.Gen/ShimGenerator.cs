using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace IFY.Shimr.Gen;

[Generator]
internal class ShimGenerator : ISourceGenerator
{
    // Replacable factory (for testing)
    internal Func<StringBuilder, ShimWriter> GetShimWriter { get; set; } = (src) => new ShimWriter(src);

    [ExcludeFromCodeCoverage] // Cannot set GeneratorExecutionContext.SyntaxContextReceiver
    public void Execute(GeneratorExecutionContext context)
    {
        // Only continue if our receiver is in use
        if (context.SyntaxContextReceiver is ShimTypeFinder receiver)
        {
            Execute(receiver.ShimTypes, context.AddSource);
        }
    }
    public void Execute(IEnumerable<ShimTypeDefinition> shimTypes, Action<string, SourceText> addSource)
    {
        var src = new StringBuilder();
        var writer = GetShimWriter(src);

#if DEBUG
        const string GenOut_File = @"C:\dev\_GH\IFY.Shimr\IFY.Shimr.Gen\GeneratorOutput.txt";
        File.WriteAllText(GenOut_File, "");
#endif

        // Generate each shim
        Debugger.Log(1, typeof(ShimGenerator).FullName, $"Generating shims ({shimTypes.Count()})...\r\n");
        foreach (var shim in shimTypes)
        {
            shim.ShimrName = $"{shim.ShimSafeName}__{shim.TargetSafeName}";
            writer.CreateShim(shim);

            // Add to the compilation
            Debugger.Log(1, typeof(ShimGenerator).FullName, $"Generated shim for {shim.ShimNamespace}.{shim.ShimName}\r\n");
            addSource(shim.ShimName + "Shim", SourceText.From(src.ToString(), Encoding.UTF8));
#if DEBUG
            File.AppendAllText(GenOut_File, src.ToString());
#endif
            src.Clear();
        }

        // Generate shim extensions
        Debugger.Log(1, typeof(ShimGenerator).FullName, "Generating Shimr extension methods...\r\n");
        foreach (var shims in shimTypes.GroupBy(s => s.TargetFullName))
        {
            writer.CreateExtensionMethod(shims.ToArray());
        }
        addSource("ShimrExtensions", SourceText.From(src.ToString(), Encoding.UTF8));
#if DEBUG
        File.AppendAllText(GenOut_File, src.ToString());
#endif

        Debugger.Log(1, typeof(ShimGenerator).FullName, "Shimr generation complete.\r\n");
    }

    [ExcludeFromCodeCoverage] // Cannot check registrations
    public void Initialize(GeneratorInitializationContext context)
        => Initialize(context.RegisterForSyntaxNotifications);
    public void Initialize(Action<SyntaxContextReceiverCreator> registerCreator)
    {
        //Debugger.Launch(); // NOTE: Comment out and re-build before closing VS

        Debugger.Log(1, typeof(ShimGenerator).FullName, "Parsing source for Shimr targets...\r\n");
        registerCreator(() => new ShimTypeFinder());
    }
}