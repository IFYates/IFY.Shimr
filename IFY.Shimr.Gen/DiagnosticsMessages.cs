using Microsoft.CodeAnalysis;

namespace IFY.Shimr.Gen;

internal static class DiagnosticsMessages
{
    #region Report
    private static readonly List<Diagnostic> _diagnostics = new();
    private static void Report(this ISymbol symbol, DiagnosticDescriptor descriptor, params object?[]? args)
    {
        _diagnostics.Add(Diagnostic.Create(descriptor, symbol.Locations.First(), symbol.Locations.Skip(1), args));
    }

    public static void PublishDiagnostics(GeneratorExecutionContext context)
    {
        foreach (var diagnostic in _diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }
    #endregion Report

    private static readonly DiagnosticDescriptor _proxyMemberNotStatic = new("SHIMR-50", "Non-static ShimProxy target", "Shim interface '{0}' contains proxy for member '{1}' to non-static '{2}.{3}'. Proxy targets must be static.", "Shimr.Gen", DiagnosticSeverity.Error, true);
    public static void ReportProxyMemberNotStatic(this ISymbol symbol, string shimTypeName, string shimMemberName, string proxyTypeName, string proxyMemberName)
        => Report(symbol, _proxyMemberNotStatic, shimTypeName, shimMemberName, proxyTypeName, proxyMemberName);
}