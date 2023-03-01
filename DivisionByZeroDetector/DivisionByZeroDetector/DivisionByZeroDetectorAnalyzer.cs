using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DivisionByZeroDetector
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DivisionByZeroDetectorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DivisionByZeroDetector";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Error";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, 
            Title, MessageFormat, Category, DiagnosticSeverity.Error, 
            isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { 
            get { return ImmutableArray.Create(Rule); } 
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeDivideNode, SyntaxKind.DivideExpression);
            context.RegisterSyntaxNodeAction(AnalyzeAssignmentExpressionNode,
                SyntaxKind.DivideAssignmentExpression);
        }

        private void AnalyzeDivideNode(SyntaxNodeAnalysisContext context)
        {
            var binaryExpression = (BinaryExpressionSyntax)context.Node;

            // Checking whether the divisor is not a constant equal to zero
            if (!context.SemanticModel.GetConstantValue(binaryExpression.Right).Value.Equals(0))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }

        private void AnalyzeAssignmentExpressionNode(SyntaxNodeAnalysisContext context)
        {
            var assignmentExpression = (AssignmentExpressionSyntax)context.Node;

            // Checking whether the divisor is not a constant equal to zero
            if (!context.SemanticModel.GetConstantValue(assignmentExpression.Right).Value.Equals(0))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }
    }
}
