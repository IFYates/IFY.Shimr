using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen;

[Generator]
internal class ShimrSourceGenerator : ISourceGenerator
{
    private readonly CodeErrorReporter _errors = new();
    private readonly ShimRegister _shimRegister = new();

    // Only called after a rebuild
    public void Initialize(GeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch(); // Do not leave uncommented when exiting VS
        Diag.WriteOutput($"// Start code generation: {DateTime.Now:o}\r\n", false); // TODO: wrong place to reset file

        var shimUseResolver = new ShimResolver(_errors, _shimRegister);
        context.RegisterForSyntaxNotifications(new SyntaxContextReceiverCreator(() => shimUseResolver));
    }

    // Called on each code change
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ShimResolver)
        {
            return;
        }

        try
        {
            _errors.SetContext(context);
            doExecute(context);
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
    private void doExecute(GeneratorExecutionContext context)
    {
        var shims = _shimRegister.ResolveAllShims();

        // Meta info
        var code = new StringBuilder();
        code.AppendLine($"// Generation time: {DateTime.Now:o}");
        code.AppendLine($"// Project: {context.Compilation.AssemblyName}");
        code.AppendLine("// Shim map:");
        foreach (var shimt in _shimRegister.Interfaces)
        {
            code.AppendLine($"// + {shimt.InterfaceFullName}");
            foreach (var shim in shimt.Shims)
            {
                code.Append($"//   - {shim.UnderlyingFullName}")
                    .AppendLine(shim is ShimFactoryModel ? " (Factory)" : null);
            }
        }
        addSource("_meta.g.cs", code);

        if (!shims.Any())
        {
            Diag.WriteOutput($"// Did not find any uses of Shimr");
            return;
        }

        var builder = new AutoShimCodeWriter(context, _errors);
        builder.WriteFactoryClass(code, shims);
        addSource("ShimBuilder.g.cs", code);

        builder.WriteExtensionClass(code, shims);
        addSource("ShimAuto.g.cs", code);

        foreach (var shim in shims)
        {
            builder.WriteShim(code, shim);
            addSource($"Shimr.{shim.Name}.g.cs", code);
        }

        Diag.WriteOutput($"// Code generation complete: {DateTime.Now:o}");

        void addSource(string name, StringBuilder code)
        {
            context.AddSource(name, code.ToString());
            Diag.WriteOutput($"/** File: {name} **/\r\n{code}");
            code.Clear();
        }
    }
}
