using IFY.Shimr.SourceGen.CodeAnalysis;
using IFY.Shimr.SourceGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen;

[Generator]
internal class ShimrSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Diag.Debug(); // NOTE: Comment out and re-build before closing VS
        //Diag.IsOutputEnabled = true;
        //Diag.WriteOutput($"// Start code generation: {DateTime.Now:o}\r\n", false); // TODO: wrong place to reset file

        var resolver = new ShimResolver();
        var provider = context.SyntaxProvider.CreateSyntaxProvider(resolver.ShouldProcess, resolver.Process);
        context.RegisterSourceOutput(provider, generateShimClass);
    }

    private void generateShimClass(SourceProductionContext context, IBinding binding)
    {
        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            doExecute(context, binding);
        }
        catch (Exception ex)
        {
            context.CodeGenError(ex);
            throw;
        }
    }
    private void doExecute(SourceProductionContext context, IBinding binding)
    {
        var writer = new GlobalCodeWriter(context);
        //GlobalCodeWriter.WriteExtensionClass(writer, allBindings);
        //GlobalCodeWriter.WriteFactoryClass(writer, allBindings);

        //context.AddSource(binding.Name + ".g.cs", writer.ToClass(binding));
    }
}
