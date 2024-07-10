using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace IFY.Shimr.CodeGen.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class OnlyShimInterfacesAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor ShimToNonInterface = new(
        id: "SHIMR101",
        title: "Invalid Shim type",
        messageFormat: "Target Shim type is not an interface: {0}",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ShimToUnknownInterface = new(
        id: "SHIMR001",
        title: "Unknown Shim type",
        messageFormat: "Referenced Shim interface type has not been registered: {0}",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [ShimToNonInterface, ShimToUnknownInterface];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(analyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void analyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invokeExpr = (InvocationExpressionSyntax)context.Node;
        var membAccessExpr = (MemberAccessExpressionSyntax)invokeExpr.Expression;

        // Only process Shim<T>() invocations
        if (invokeExpr.ArgumentList.Arguments.Count > 0
            || membAccessExpr.Name is not GenericNameSyntax name
            || name.TypeArgumentList.Arguments.Count != 1
            || name.TypeArgumentList.Arguments[0] is not TypeSyntax argType
            || name.Identifier.ValueText != "Shim")
        {
            return;
        }

        // Must be in generated code
        var typeInfo = context.SemanticModel.GetTypeInfo(argType);
        var memberSymbolInfo = context.SemanticModel.GetSymbolInfo(membAccessExpr.Name);
        var containingType = memberSymbolInfo.Symbol!.ContainingType.ToDisplayString();
        if (typeInfo.Type == null || containingType != AutoShimBuilder.CG_CLASSNAMEFULL)
        {
            return;
        }

        // Error for non-interface, Warning for unknown interface
        var typeName = typeInfo.Type.ToDisplayString();
        if (typeInfo.Type.TypeKind != TypeKind.Interface)
        {
            context.ReportDiagnostic(Diagnostic.Create(ShimToNonInterface, context.Node.GetLocation(), typeName));
        }
        else if (!ShimrSourceGenerator.KnownShims.Contains(typeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(ShimToUnknownInterface, context.Node.GetLocation(), typeName));
        }
    }
}
