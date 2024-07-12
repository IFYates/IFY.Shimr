using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace IFY.Shimr.CodeGen;

[Generator]
internal class ShimrSourceGenerator : ISourceGenerator
{
    // Only called after a rebuild
    public void Initialize(GeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch(); // Do not leave uncommented when exiting VS
        Diag.IsEnabled = !Assembly.GetExecutingAssembly().Location.Contains("\\Temp\\");
        Diag.WriteOutput($"// Start code generation: {DateTime.Now:o}\r\n", false); // TODO: wrong place to reset file

        context.RegisterForSyntaxNotifications(new SyntaxContextReceiverCreator(() => new ShimResolver()));
    }

    // Called on each code change
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ShimResolver resolver)
        {
            return;
        }

        try
        {
            resolver.Errors.SetContext(context);
            doExecute(context, resolver.Errors, resolver.Shims);
        }
        catch (Exception ex)
        {
            var err = $"{ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}";
            context.AddSource("ERROR.log.cs", $"// {err}");
            Diag.WriteOutput($"// ERROR: {err}");
            // TODO: _errors.CodeGenFailed(ex);
            throw;
        }
    }
    private void doExecute(GeneratorExecutionContext context, CodeErrorReporter errors, ShimRegister shimRegister)
    {
        var shims = shimRegister.ResolveAllShims();

        // Meta info
        var code = new StringBuilder();
        code.AppendLine($"// Generation time: {DateTime.Now:o}");
        code.AppendLine($"// Project: {context.Compilation.AssemblyName}");
        code.AppendLine($"// Shim map ({shims.Length}):");
        foreach (var shimt in shimRegister.Types)
        {
            code.AppendLine($"// + {shimt.InterfaceFullName}");
            foreach (var shim in shimt.Shims)
            {
                code.Append($"//   - {shim.UnderlyingFullName}")
                    .AppendLine(shim is ShimFactoryTarget ? " (Factory)" : null);
            }
        }
        addSource("_meta.g.cs", code);

        if (!shims.Any())
        {
            Diag.WriteOutput($"// Did not find any uses of Shimr");
            return;
        }

        var builder = new AutoShimCodeWriter(context);
        builder.WriteFactoryClass(code, shims);
        addSource("ShimBuilder.g.cs", code);

        builder.WriteExtensionClass(code, shims);
        addSource("ObjectExtensions.g.cs", code);

        foreach (var shimType in shimRegister.Types)
        {
            shimType.GenerateCode(code, errors);
            addSource($"Shimr.{shimType.Name}.g.cs", code);
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
