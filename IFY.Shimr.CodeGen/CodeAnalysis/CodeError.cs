using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

/// <summary>
/// Handles raising build errors.
/// </summary>
internal class CodeError
{
    private readonly Queue<Diagnostic> _diagnostics = new();
    private void reportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
    {
        var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
        lock (_diagnostics)
        {
            if (_context.HasValue)
            {
                _context.Value.ReportDiagnostic(diagnostic);
            }
            else
            {
                _diagnostics.Enqueue(diagnostic);
            }
        }
    }

    private GeneratorExecutionContext? _context;
    public void SetContext(GeneratorExecutionContext context)
    {
        lock (_diagnostics)
        {
            while (_diagnostics.Count > 0)
            {
                context.ReportDiagnostic(_diagnostics.Dequeue());
            }
            _context = context;
        }
    }

    //public void UnknownShimWarning(SyntaxNode node, ITypeSymbol? shimdType)
    //{
    //    context.ReportDiagnostic(Diagnostic.Create(ShimToUnknownInterface, node.GetLocation(), shimdType?.ToDisplayString() ?? "Unknown"));
    //}
    //public static readonly DiagnosticDescriptor ShimToUnknownInterface = new(
    //    id: "SHIMR001",
    //    title: "Unknown Shim type",
    //    messageFormat: "'{0}' has not been registered as a shim type",
    //    category: "Correctness",
    //    defaultSeverity: DiagnosticSeverity.Warning,
    //    isEnabledByDefault: true
    //);

    public void NoTypeWarning(SyntaxNode node, ITypeSymbol? shimdType)
    {
        reportDiagnostic(ShimToUnknownInterface, node.GetLocation(), shimdType?.ToDisplayString() ?? "Unknown");
    }
    public static readonly DiagnosticDescriptor ShimToUnknownInterface = new(
        id: "SHIMR002",
        title: "Undetermined Shim type",
        messageFormat: "'{0}' is not a determinable type and won't be registered as a shimmable type",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public void NonInterfaceError(SyntaxNode node, ITypeSymbol? argType)
    {
        reportDiagnostic(ShimToNonInterface, node.GetLocation(), argType?.ToDisplayString() ?? "Unknown");
    }
    public static readonly DiagnosticDescriptor ShimToNonInterface = new(
        id: "SHIMR101",
        title: "Invalid Shim type",
        messageFormat: "'{0}' is not an interface and invalid as a shim type",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
