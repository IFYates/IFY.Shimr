﻿using Microsoft.CodeAnalysis;

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

    // TODO: missing named member of same type
    // TODO: no matching parameter count
    // TODO: invalid accessibility
    // TODO: shim return type must be interface
    // TODO: unable to shim return type
    // TODO: shim parameter type must be interface
    // TODO: unable to shim parameter type
    // TODO: member collision - needs explicit naming
    // TODO: missing type arguments
    // TODO: static/non-static mismatch
    // TODO: proxy add existing member
    // TODO: proxy override missing member

    private static readonly DiagnosticDescriptor _conflictingAttributes = new("SHIMR-10", "Non-static ShimProxy target", "Shim interface '{0}' contains conflicting attributes on member '{1}'. Cannot use '{2}' and '{3}' together.", "Shimr.Gen", DiagnosticSeverity.Error, true);
    public static void ReportConflictingAttributes(this ISymbol symbol, string shimTypeName, string? shimMemberName, string attribute1, string attribute2)
        => Report(symbol, _conflictingAttributes, shimTypeName, shimMemberName ?? "(class)", attribute1, attribute2);

    //private static readonly DiagnosticDescriptor _proxyMemberNotStatic = new("SHIMR-50", "Non-static ShimProxy target", "Shim interface '{0}' contains proxy for member '{1}' to non-static '{2}.{3}'. Proxy targets must be static.", "Shimr.Gen", DiagnosticSeverity.Error, true);
    //public static void ReportProxyMemberNotStatic(this ISymbol symbol, string shimTypeName, string shimMemberName, string proxyTypeName, string proxyMemberName)
    //    => Report(symbol, _proxyMemberNotStatic, shimTypeName, shimMemberName, proxyTypeName, proxyMemberName);
}