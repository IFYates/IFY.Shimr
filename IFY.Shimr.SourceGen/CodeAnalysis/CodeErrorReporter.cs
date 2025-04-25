using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen.CodeAnalysis;

/// <summary>
/// Handles raising build errors.
/// </summary>
internal static class CodeErrorReporter
{
    public static void NoTypeWarning(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(ShimrNotUsed, null));
    }
    public static readonly DiagnosticDescriptor ShimrNotUsed = new(
        id: "SHIMR000",
        title: "No uses of Shimr found",
        messageFormat: "No uses of Shimr were found in the code",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    //public static void UnknownShimWarning(this SourceProductionContext context, SyntaxNode node, ITypeSymbol? shimdType)
    //{
    //    context.ReportDiagnostic(Diagnostic.Create(ShimToUnknownInterface, node.GetLocation(), shimdType?.ToDisplayString() ?? "Unknown"));
    //}
    //public static readonly DiagnosticDescriptor ShimToUnknownInterface = new(
    //    id: "SHIMR101",
    //    title: "Unknown Shim type",
    //    messageFormat: "'{0}' has not been registered as a shim type",
    //    category: "Correctness",
    //    defaultSeverity: DiagnosticSeverity.Warning,
    //    isEnabledByDefault: true
    //);

    public static void NoTypeWarning(this SourceProductionContext context, SyntaxNode node, ITypeSymbol? shimdType)
    {
        context.ReportDiagnostic(Diagnostic.Create(ShimToUnknownInterface, node.GetLocation(), shimdType?.ToDisplayString() ?? "Unknown"));
    }
    public static readonly DiagnosticDescriptor ShimToUnknownInterface = new(
        id: "SHIMR102",
        title: "Undetermined Shim type",
        messageFormat: "'{0}' is not a determinable type and won't be registered as a shimmable type",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static void CodeGenError(this SourceProductionContext context, Exception ex)
    {
        context.ReportDiagnostic(Diagnostic.Create(FailedToGenerateCode, null, ex));
    }
    public static readonly DiagnosticDescriptor FailedToGenerateCode = new(
        id: "SHIMR200",
        title: "Failed to generate shims",
        messageFormat: "There was an unexpected error generating shims: {0}",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static void NonInterfaceError(this SourceProductionContext context, SyntaxNode node, ITypeSymbol? argType)
    {
        context.ReportDiagnostic(Diagnostic.Create(ShimToNonInterface, node.GetLocation(), argType?.ToDisplayString() ?? "Unknown"));
    }
    public static readonly DiagnosticDescriptor ShimToNonInterface = new(
        id: "SHIMR201",
        title: "Invalid Shim type",
        messageFormat: "'{0}' is not an interface and invalid as a shim type",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static void InterfaceUseError(this SourceProductionContext context, SyntaxNode node, ITypeSymbol? argType)
    {
        context.ReportDiagnostic(Diagnostic.Create(NeedNonInterface, node.GetLocation(), argType?.ToDisplayString() ?? "Unknown"));
    }
    public static readonly DiagnosticDescriptor NeedNonInterface = new(
        id: "SHIMR202",
        title: "Interface type not allowed",
        messageFormat: "'{0}' is an interface and invalid for use here",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static void NoMemberError(this SourceProductionContext context, ITypeSymbol targetType, ISymbol shimMember)
    {
        context.ReportDiagnostic(Diagnostic.Create(ShimOfUnknownMember, targetType.GetSyntaxNode()?.GetLocation(), targetType.ToFullName(), $"{shimMember.ContainingType.ToFullName()}.{shimMember.Name}"));
    }
    public static readonly DiagnosticDescriptor ShimOfUnknownMember = new(
        id: "SHIMR203",
        title: "Unable to resolve target member",
        messageFormat: "Shim target '{0}' does not contain member '{1}' and missing members are currently fatal",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static void InvalidReturnTypeError(this SourceProductionContext context, SyntaxNode? node, string shimMember, string returnType)
    {
        context.ReportDiagnostic(Diagnostic.Create(InvalidShimReturnType, node?.GetLocation(), shimMember, returnType));
    }
    public static readonly DiagnosticDescriptor InvalidShimReturnType = new(
        id: "SHIMR204",
        title: "Invalid return type",
        messageFormat: "Shim target '{0}' has invalid return type '{1}'",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
