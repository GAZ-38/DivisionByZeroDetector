using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(
            nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Error";
        

        class VariableChangeData
        {
            public ISymbol Name;
            public int Span;
            public bool IsZero;

            public VariableChangeData()
            {
                this.IsZero = false;
            }
            public VariableChangeData(ISymbol name, int span, bool isZero)
            {
                this.Name = name;
                this.Span = span;
                this.IsZero = isZero;
            }
        }

        private List<VariableChangeData> variableChanges = new List<VariableChangeData>();

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

            // Analyzers for division
            context.RegisterSyntaxNodeAction(AnalyzeDivideNode, SyntaxKind.DivideExpression);
            context.RegisterSyntaxNodeAction(AnalyzeDivideAssignmentExpressionNode,
                SyntaxKind.DivideAssignmentExpression);
            
            // Analyzers for assignment and declaration
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclarationNode, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzeSimpleAssignmentExpressionNode,
                SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeComplexAssignmentExpressionNode,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression, 
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression);
        }

        private void AnalyzeLocalDeclarationNode(SyntaxNodeAnalysisContext context)
        {
            ISymbol name;
            int span;
            bool isZero;

            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;
            foreach (VariableDeclaratorSyntax variable in localDeclaration.Declaration.Variables)
            {
                //Сhecking whether the variable is not initialized
                EqualsValueClauseSyntax initializer = variable.Initializer;
                if (initializer == null)
                {
                    continue;
                }
                //checking whether the variable is initialized by constant
                Optional <object> constantValue = context.SemanticModel.GetConstantValue(initializer.Value, context.CancellationToken);
                if (constantValue.HasValue)
                {
                    //checking whether the variable is initialized by zero
                    isZero = constantValue.Value.Equals(0);
                }
                else
                {
                    continue;
                }

                name = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken);

                span = variable.SpanStart;

                //Addition of information about the variable to the list of changes
                VariableChangeData changeData = new VariableChangeData(name, span, isZero);
                variableChanges.Add(changeData);
            }
        }

        private void AnalyzeSimpleAssignmentExpressionNode(SyntaxNodeAnalysisContext context)
        {
            ISymbol name;
            int span;
            bool isZero;

            var assignmentExpression = (AssignmentExpressionSyntax)context.Node;
            var constantValue = context.SemanticModel.GetConstantValue(assignmentExpression.Right, context.CancellationToken);

            // Checking whether constant value is assigned to a variable
            if (!constantValue.HasValue)
            {
                return;
            }

            // Checking whether zero is assigned to a variable
            isZero = constantValue.Value.Equals(0);
           
            name = context.SemanticModel.GetSymbolInfo(assignmentExpression.Left, context.CancellationToken).Symbol;

            span = assignmentExpression.SpanStart;

            //Addition of information about the variable to the list of changes
            VariableChangeData changeData = new VariableChangeData(name, span, isZero);
            variableChanges.Add(changeData);
        }

        private void AnalyzeComplexAssignmentExpressionNode(SyntaxNodeAnalysisContext context)
        {
            // If assignment operator is not simple, the value of the zero variable could be changed
            var assignmentExpression = (AssignmentExpressionSyntax)context.Node;

            ISymbol name = context.SemanticModel.GetSymbolInfo(assignmentExpression.Left, context.CancellationToken).Symbol;
            int span = assignmentExpression.SpanStart;
            bool isZero = false;

            //Addition of information about the variable to the list of changes
            VariableChangeData changeData = new VariableChangeData(name, span, isZero);
            variableChanges.Add(changeData);
        }


        private void AnalyzeDivideNode(SyntaxNodeAnalysisContext context)
        {
            var divisionExpression = (BinaryExpressionSyntax)context.Node;

            if (context.SemanticModel.GetConstantValue(divisionExpression.Right).HasValue.Equals(false))
            {
                // Divisor is not a constant:
                // checking whether the last operation with a divisor is assignment of zero
                if (divisionExpression.Right.IsKind(SyntaxKind.IdentifierName))
                {
                    ISymbol name = context.SemanticModel.GetSymbolInfo(divisionExpression.Right, context.CancellationToken).Symbol;
                    
                    int span = divisionExpression.SpanStart;
                    bool isDividedByZero = false;
                    foreach (VariableChangeData changeData in variableChanges)
                    {
                        if (changeData.Span >= span) break;
                        if (!SymbolEqualityComparer.Default.Equals(name, changeData.Name)) continue;

                        isDividedByZero = changeData.IsZero;
                    }
                    if (!isDividedByZero) { 
                        return; 
                    }
                }
                else { return; }
            }
            // Divisor is a constant: checking whether the divisor is zero
            else if (!context.SemanticModel.GetConstantValue(divisionExpression.Right).Value.Equals(0))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }

        private void AnalyzeDivideAssignmentExpressionNode(SyntaxNodeAnalysisContext context)
        {
            var divisionExpression = (AssignmentExpressionSyntax)context.Node;

            if (context.SemanticModel.GetConstantValue(divisionExpression.Right).HasValue.Equals(false))
            {
                // Divisor is not a constant:
                // checking whether the last operation with a divisor is assignment of zero
                if (divisionExpression.Right.IsKind(SyntaxKind.IdentifierName))
                {
                    ISymbol name = context.SemanticModel.GetSymbolInfo(divisionExpression.Right, context.CancellationToken).Symbol;

                    int span = divisionExpression.SpanStart;
                    bool isDividedByZero = false;
                    foreach (VariableChangeData changeData in variableChanges)
                    {
                        if (changeData.Span >= span) break;
                        if (!SymbolEqualityComparer.Default.Equals(name, changeData.Name)) continue;

                        isDividedByZero = changeData.IsZero;
                    }
                    if (!isDividedByZero) {
                        return;
                    }
                }
                else { return; }
            }
            // Divisor is a constant: checking whether the divisor is zero
            else if (!context.SemanticModel.GetConstantValue(divisionExpression.Right).Value.Equals(0))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }
    }
}
