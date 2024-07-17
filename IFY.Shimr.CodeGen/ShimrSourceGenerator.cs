using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen;

// TODO: https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md#progressive-complexity-opt-in
[Generator]
internal class ShimrSourceGenerator : ISourceGenerator
{
    // Only called after a rebuild
    public void Initialize(GeneratorInitializationContext context)
    {
        Diag.IsEnabled = true;// !Assembly.GetExecutingAssembly().Location.Contains("\\Temp\\");
        Diag.WriteOutput($"// Start code generation: {DateTime.Now:o}\r\n", false); // TODO: wrong place to reset file
        //Diag.WriteOutput($"// {Assembly.GetExecutingAssembly().Location}");

        // TODO: This approach does not support multiple target frameworks
        context.RegisterForSyntaxNotifications(() => new ShimResolver());
    }

    // Called on each code change
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.ProjectDir", out var projectDir))
        {
            Diag.OutputPath = projectDir;
        }

        if (context.SyntaxContextReceiver is not ShimResolver resolver)
        {
            return;
        }

        try
        {
            resolver.Errors.SetContext(context);
            doExecute(context, resolver.Errors, resolver);
        }
        catch (Exception ex)
        {
            var err = $"{ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}";
            context.AddSource("ERROR.log.cs", $"// {err}");
            Diag.WriteOutput($"// ERROR: {err}");
            resolver.Errors.CodeGenError(ex);
            throw;
        }
    }
    private void doExecute(GeneratorExecutionContext context, CodeErrorReporter errors, ShimResolver shimResolver)
    {
        // Resolve all shim bindings
        var allBindings = shimResolver.ResolveAllShims(errors);

        // Meta info
        var code = new StringBuilder();
        code.AppendLine($"// Project: {context.Compilation.AssemblyName}");
        code.AppendLine($"// Shim map ({allBindings.Count}):");
        foreach (var def in allBindings.GroupBy(b => b.Definition))
        {
            code.AppendLine($"// + {def.Key.FullTypeName} ({def.Key.GetType().Name})");
            foreach (var shim in def.Select(d => d.Target).Distinct())
            {
                code.AppendLine($"//   - {shim.FullTypeName} ({shim.GetType().Name})");
            }
        }
        addSource("_meta.g.cs", code);

        var writer = new GlobalCodeWriter(context);
        GlobalCodeWriter.WriteExtensionClass(writer, allBindings);

        if (!allBindings.Any())
        {
            Diag.WriteOutput($"// Did not find any uses of Shimr");
            return;
        }

        GlobalCodeWriter.WriteFactoryClass(writer, allBindings);

        var bindingsByDefinition = allBindings.GroupBy(b => b.Definition).ToArray();
        foreach (var group in bindingsByDefinition)
        {
            group.Key.WriteShimClass(writer, group);
        }

        Diag.WriteOutput($"// Code generation complete: {DateTime.Now:o}");

        void addSource(string name, StringBuilder code)
        {
            code.Insert(0, $"// Generated at {DateTime.Now:O}\r\n");
            context.AddSource(name, code.ToString());
            Diag.WriteOutput($"/** File: {name} **/\r\n{code}");
            code.Clear();
        }
    }
}
