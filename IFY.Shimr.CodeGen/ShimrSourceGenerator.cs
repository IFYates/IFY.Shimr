using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IFY.Shimr.CodeGen;

[Generator]
internal class ShimrSourceGenerator : ISourceGenerator
{
    private const string OUTPUT_FILE = "F:\\Dev\\IFY.Shimr\\Sourcegen.cs";
    public static void WriteOutput(string text, bool append = true)
    {
#if DEBUG
        using FileStream fs = new(OUTPUT_FILE, append ? FileMode.Append : FileMode.Create);
        using StreamWriter sw = new(fs);
        try
        {
            sw.WriteLine(text);
        }
        finally
        {
            sw.Close();
        }
#endif
    }

    public static string[] KnownShims { get; private set; } = [];

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            WriteOutput($"// Project {context.Compilation.AssemblyName}");
            doExecute(context);
        }
        catch (Exception ex)
        {
            WriteOutput($"/** {ex.GetType().FullName}: {ex.Message}");
            WriteOutput(ex.StackTrace);
            WriteOutput("**/");
            throw;
        }
    }
    private void doExecute(GeneratorExecutionContext context)
    {
        // TODO: find uses of .Shim<T>() and drop attribute

        var builder = new AutoShimBuilder(context);

        // Find all uses of new attribute
        var interfaces = context.Compilation.SyntaxTrees
            .SelectMany(st => st.GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()).ToArray();
        var discoveredShims = interfaces
            .SelectMany(builder.GetInterfaceShims)
            .Distinct().ToArray();
        if (!discoveredShims.Any())
        {
            WriteOutput($"// Did not find any uses of {builder.AttributeSymbol.Name} on {interfaces.Length} interfaces");
            return;
        }

        KnownShims = discoveredShims.Select(s => s.InterfaceFullName).ToArray();

        var code = new StringBuilder();
        builder.BuildFactoryClass(code);
        addSource("ShimBuilder.g.cs", code);

        var shims = discoveredShims.SelectMany(s => s.Shims).ToArray();
        builder.BuildExtensionClass(code, shims);
        addSource("ShimAuto.g.cs", code);

        foreach (var shim in shims)
        {
            builder.BuildShimClass(code, shim);
            addSource($"Shimr.{shim.Name}.g.cs", code);
        }

        void addSource(string name, StringBuilder code)
        {
            context.AddSource(name, code.ToString());
            WriteOutput($"/** File: {name} **/\r\n{code}");
            code.Clear();
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        //Debugger.Launch();
        WriteOutput($"// Auto-generated code ({DateTime.Now:o})\r\n", false);

        // TODO: use ISyntaxReceiver?
    }

    //class ShimrReceiver : ISyntaxReceiver
    //{
    //    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    //    {
    //        if (syntaxNode is ClassDeclarationSyntax cds
    //            && cds.HaveAttribute(ShimrAttribute))
    //        {
    //        }
    //    }
    //}
}
