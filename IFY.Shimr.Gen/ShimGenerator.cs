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
                shimTypes.Add(new ShimTypeDefinition(shimType.ShimType, shimType.TargetType, false));
            }
        }

#if DEBUG
        const string GenOut_File = @"C:\dev\_GH\IFY.Shimr\IFY.Shimr.Gen\GeneratorOutput.txt";
        File.WriteAllText(GenOut_File, $"// {DateTime.Now.ToString("O")}\r\n");
#endif

        // Generate each shim
        Debugger.Log(1, typeof(ShimGenerator).FullName, $"Generating shims ({shimTypes.Count()})...\r\n");
        foreach (var typeShims in shimTypes.GroupBy(s => s.TargetFullName))
        {
            var targetType = typeShims.Key;
            var shims = typeShims.ToArray();
            writer.CreateShim(shims);

            // Add to the compilation
            Debugger.Log(1, typeof(ShimGenerator).FullName, $"Generated {shims.Length} shim(s) for {targetType}\r\n");
            addSource(shims[0].TargetSafeName + "Shims", SourceText.From(src.ToString(), Encoding.UTF8));
#if DEBUG
            File.AppendAllText(GenOut_File, src.ToString());
#endif
            src.Clear();
        }

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