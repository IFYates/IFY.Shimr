using IFY.Shimr.SourceGen.CodeAnalysis;
using IFY.Shimr.SourceGen.Models;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen;

[Generator]
internal class ShimrSourceGenerator : IIncrementalGenerator
{
    private static ShimResolver _resolver = null!;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Diag.Debug(); // NOTE: Comment out and re-build before closing VS
        //Diag.IsOutputEnabled = true;
        //Diag.WriteOutput($"// Start code generation: {DateTime.Now:o}\r\n", false); // TODO: wrong place to reset file

        _resolver ??= new ShimResolver();
        var provider = context.SyntaxProvider.CreateSyntaxProvider(_resolver.FindShimMethods, _resolver.ProcessShimMethods);
        context.RegisterSourceOutput(provider, generateOutput);
    }

    // TODO: does this happen per Find or at end for all?
    private void generateOutput(SourceProductionContext context, IShimDefinition binding)
    {
        if (context.CancellationToken.IsCancellationRequested || binding == null)
        {
            return;
        }

        try
        {
            // TODO: specific shim just once

            var bindings = _resolver.ResolveAllShims();
            var writer = new GlobalCodeWriter(context);

            GlobalCodeWriter.WriteExtensionClass(writer, bindings);
            if (!bindings.Any())
            {
                Diag.WriteOutput($"// Did not find any uses of Shimr");
                return;
            }

            //GlobalCodeWriter.WriteFactoryClass(writer, bindings);

            var bindingsByDefinition = bindings.GroupBy(b => b.Definition).ToArray();
            foreach (var group in bindingsByDefinition)
            {
                group.Key.WriteShimClass(writer, group);
            }
        }
        catch (Exception ex)
        {
            context.CodeGenError(ex);
            throw;
        }
    }
}
