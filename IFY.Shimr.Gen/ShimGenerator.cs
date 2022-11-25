﻿using IFY.Shimr.Gen.SyntaxParsing;
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
    private const string GenOut_File = @"C:\dev\_GH\IFY.Shimr\IFY.Shimr.Gen\GeneratorOutput2.txt";
    private static bool? _writeDebug;
    private static void writeToDebug(string str)
    {
        if (!_writeDebug.HasValue)
        {
            var asmFI = new FileInfo(typeof(ShimGenerator).Assembly.Location);
            var outFI = new FileInfo(GenOut_File);
            _writeDebug = !outFI.Exists || outFI.Length == 0 || outFI.LastWriteTimeUtc < asmFI.LastWriteTimeUtc;
            if (_writeDebug == true)
            {
                File.WriteAllText(GenOut_File, "");
            }
        }
        if (_writeDebug == true)
        {
            File.AppendAllText(GenOut_File, str);
        }
    }
#endif

    [ExcludeFromCodeCoverage] // Cannot set GeneratorExecutionContext.SyntaxContextReceiver
    public void Execute(GeneratorExecutionContext context)
    {
        // Only continue if our receiver is in use
        if (context.SyntaxContextReceiver is ShimTypeFinder receiver)
        {
            try
            {
                Execute(receiver.ShimTypes, context.AddSource);
            }
            catch (Exception ex)
            {
                _ = ex.ToString();
                Debugger.Launch();
                throw;
            }
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
                Debugger.Launch();
                shimTypes.Add(new ShimTypeDefinition(ShimType, TargetType, false));
            }
        }

#if DEBUG
        writeToDebug($"// {DateTime.Now:O}\r\n");

        foreach (var shim in shimTypes)
        {
            src.AppendLine($"// * {shim.TargetFullName} -> {shim.ShimFullName}");
        }
        writeToDebug(src.ToString());
        src.Clear();
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
            addSource($"{shims[0].TargetFullName.MakeSafeName()}Shims", SourceText.From(src.ToString(), Encoding.UTF8));
#if DEBUG
            writeToDebug(src.ToString());
#endif
            src.Clear();
        }

        // Generate static creators
        writer.CreateStaticShimCreator(shimTypes.Where(s => s.IsStatic).ToArray());
        addSource("StaticCreatorShims", SourceText.From(src.ToString(), Encoding.UTF8));
#if DEBUG
        writeToDebug(src.ToString());
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