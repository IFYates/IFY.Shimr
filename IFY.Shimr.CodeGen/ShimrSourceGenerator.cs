using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen;

[Generator]
internal class ShimrSourceGenerator : ISourceGenerator
{
    private readonly CodeError _errors = new();
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
        var code = new StringBuilder();

        var shims = _shimRegister.Interfaces.SelectMany(s => s.Shims ?? []).ToList();
        var newShims = shims.ToList();
        while (newShims.Count > 0)
        {
            var loop = newShims.Distinct().ToArray();
            newShims.Clear();
            foreach (var shim in loop)
            {
                shim.ResolveImplicitShims(_shimRegister, newShims);
            }
            newShims.RemoveAll(shims.Contains);
            shims.AddRange(newShims.Distinct());
        }

        // Meta info
        code.AppendLine($"// Generation time: {DateTime.Now:o}");
        code.AppendLine($"// Project: {context.Compilation.AssemblyName}");
        code.AppendLine("// Shim map:");
        foreach (var shimt in _shimRegister.Interfaces)
        {
            code.AppendLine($"// + {shimt.InterfaceFullName}");
            foreach (var shim in shimt.Shims)
            {
                code.AppendLine($"//   - {shim.UnderlyingFullName}");
            }
        }
        addSource("_meta.g.cs", code);

        if (!shims.Any())
        {
            Diag.WriteOutput($"// Did not find any uses of Shimr");
            return;
        }

        var builder = new AutoShimCodeWriter(context);
        builder.WriteFactoryClass(code);
        addSource("ShimBuilder.g.cs", code);

        builder.WriteExtensionClass(code, shims);
        addSource("ShimAuto.g.cs", code);

        foreach (var shim in shims)
        {
            builder.WriteShimClass(code, shim);
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
