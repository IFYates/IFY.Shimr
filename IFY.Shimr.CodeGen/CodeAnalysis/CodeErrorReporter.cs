using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace IFY.Shimr.CodeGen.CodeAnalysis;

/// <summary>
/// Handles raising build errors.
/// </summary>
internal class CodeErrorReporter
{
    private readonly Queue<Diagnostic> _diagnostics = new();
    private void reportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
    {
        var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
        lock (_diagnostics)
        {
            if (_context.HasValue)
            {
                reportDiagnostic(diagnostic);
            }
            else
            {
                _diagnostics.Enqueue(diagnostic);
            }
        }
    }
    private void reportDiagnostic(Diagnostic diagnostic)
    {
        try
        {
            _context!.Value.ReportDiagnostic(diagnostic);
        }
        catch (Exception ex)
        {
            var err = $"{ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}";
            _context!.Value.AddSource("ERROR.log.cs", $"// {err}");
            Diag.WriteOutput($"// ERROR: {err}");
        }
    }

    private GeneratorExecutionContext? _context;
    public void SetContext(GeneratorExecutionContext context)
    {
        lock (_diagnostics)
        {
            _context = context;
            while (_diagnostics.Count > 0)
            {
                reportDiagnostic(_diagnostics.Dequeue());
            }
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

    public void CodeGenError(Exception ex)
    {
        reportDiagnostic(FailedToGenerateCode, null, ex);
    }
    public static readonly DiagnosticDescriptor FailedToGenerateCode = new(
        id: "SHIMR100",
        title: "Failed to generate shims",
        messageFormat: "There was an unexpected error generating shims: {0}",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
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

    public void InterfaceUseError(SyntaxNode node, ITypeSymbol? argType)
    {
        reportDiagnostic(NeedNonInterface, node.GetLocation(), argType?.ToDisplayString() ?? "Unknown");
    }
    public static readonly DiagnosticDescriptor NeedNonInterface = new(
        id: "SHIMR102",
        title: "Interface type not allowed",
        messageFormat: "'{0}' is an interface and invalid for use here",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void NoMemberError(ITypeSymbol targetType, ISymbol shimMember)
    {
        reportDiagnostic(ShimOfUnknownMember, targetType.GetSyntaxNode()?.GetLocation(), targetType.ToFullName(), $"{shimMember.ContainingType.ToFullName()}.{shimMember.Name}");
    }
    public static readonly DiagnosticDescriptor ShimOfUnknownMember = new(
        id: "SHIMR103",
        title: "Unable to resolve target member",
        messageFormat: "Shim target '{0}' does not contain member '{1}' and missing members are currently fatal",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void InvalidReturnTypeError(SyntaxNode? node, string shimMember, string returnType)
    {
        reportDiagnostic(InvalidShimReturnType, node?.GetLocation(), shimMember, returnType);
    }
    public static readonly DiagnosticDescriptor InvalidShimReturnType = new(
        id: "SHIMR104",
        title: "Invalid return type",
        messageFormat: "Shim target '{0}' has invalid return type '{1}'",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
