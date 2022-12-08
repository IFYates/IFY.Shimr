using IFY.Shimr.Gen.SyntaxParsing;
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

#if DEBUG
    private const string GenOut_File = @"C:\dev\_GH\IFY.Shimr\IFY.Shimr.Gen\GeneratorOutput.txt";
    private static string _debugOutput = string.Empty;
#endif

    [ExcludeFromCodeCoverage] // Cannot set GeneratorExecutionContext.SyntaxContextReceiver
    public void Execute(GeneratorExecutionContext context)
    {
        // Only continue if our receiver is in use
        if (context.SyntaxContextReceiver is ShimTypeFinder receiver)
        {
            try
            {
#if DEBUG
                _debugOutput = $"// {DateTime.UtcNow:s}\r\n";
#endif

                if (receiver.Exception != null)
                {
                    throw new Exception(nameof(ShimTypeFinder) + " threw exception", receiver.Exception);
                }

                Execute(receiver.ShimTypes, context.AddSource);

#if DEBUG
                _debugOutput += $"\r\n//-- Complete {DateTime.UtcNow:s}";
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                _debugOutput += $"\r\n/** EXCEPTION! {ex}\r\n*/";
#endif
                //Debugger.Launch();
                //throw;
            }
#if DEBUG
            File.WriteAllText(GenOut_File, _debugOutput);
            Debugger.Log(0, nameof(ShimGenerator), _debugOutput);
#endif
        }
    }
    public void Execute(IList<ShimTypeDefinition> shimTypes, Action<string, SourceText> addSource)
    {
        var src = new StringBuilder();
        var writer = GetShimWriter(src);

        // Union with additional shims detected
        var additionalShims = shimTypes.SelectMany(s => s.AdditionalShims)
            .Distinct().ToArray();
        foreach (var (ShimType, TargetType) in additionalShims)
        {
            var shimFullName = ShimType.FullName;
            var targetFullName = TargetType.FullName;
            if (!shimTypes.Any(t => t.ShimFullName == shimFullName && t.TargetFullName == targetFullName))
            {
                shimTypes.Add(new ShimTypeDefinition(ShimType, TargetType, false));
            }
        }

#if DEBUG
        foreach (var shim in shimTypes)
        {
            _debugOutput += $"// * {shim.TargetFullName} -> {shim.ShimFullName}\r\n";
        }
#endif

        // Generate each shim
        Debugger.Log(1, typeof(ShimGenerator).FullName, $"Generating shims ({shimTypes.Count()})...\r\n");
        foreach (var typeShims in shimTypes.GroupBy(s => s.TargetFullName))
        {
            var targetType = typeShims.Key;
            var shims = typeShims.ToArray();
            writer.CreateTargetShims(shims[0].TargetType, shims);

            // Add to the compilation
            Debugger.Log(1, typeof(ShimGenerator).FullName, $"Generated {shims.Length} shim(s) for {targetType}\r\n");
            addSource($"{shims[0].TargetSafeName}Shims", SourceText.From(src.ToString(), Encoding.UTF8));
#if DEBUG
            _debugOutput += $"\r\n//-- {shims[0].TargetSafeName}\r\n";
            _debugOutput += src.ToString();
#endif
            src.Clear();
        }

        // Generate static creators
        writer.CreateStaticShimCreator(shimTypes.Where(s => s.IsStatic).ToArray());
        addSource("StaticCreatorShims", SourceText.From(src.ToString(), Encoding.UTF8));
#if DEBUG
        _debugOutput += "\r\n//-- StaticCreatorShims\r\n";
        _debugOutput += src.ToString();
#endif
        src.Clear();

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