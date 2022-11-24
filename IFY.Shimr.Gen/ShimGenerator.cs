using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tortuga.TestMonkey;

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
    public void Execute(IList<ShimTypeDefinition> shimTypes, Action<string, SourceText> addSource)
    {
        var src = new StringBuilder();
        var writer = GetShimWriter(src);

        // Union with additional shims detected
        var additionalShims = shimTypes.SelectMany(s => s.AdditionalShims)
            .Distinct().ToArray();
        foreach (var shimType in additionalShims)
        {
            var shimFullName = shimType.ShimType.FullName();
            var targetFullName = shimType.TargetType.FullName();
            if (!shimTypes.Any(t => t.ShimFullName == shimFullName && t.TargetFullName == targetFullName))
            {
                shimTypes.Add(new ShimTypeDefinition(shimType.ShimType, shimType.TargetType));
            }
        }

#if DEBUG
        const string GenOut_File = @"C:\dev\_GH\IFY.Shimr\IFY.Shimr.Gen\GeneratorOutput2.txt";
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
        Debugger.Log(1, typeof(ShimGenerator).FullName, "Parsing source for Shimr targets...\r\n");
        registerCreator(() => new ShimTypeFinder());
    }
}