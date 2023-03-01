using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading.Tasks;
using VerifyCS = DivisionByZeroDetector.Test.CSharpCodeFixVerifier<
    DivisionByZeroDetector.DivisionByZeroDetectorAnalyzer,
    DivisionByZeroDetector.DivisionByZeroDetectorCodeFixProvider>;

namespace DivisionByZeroDetector.Test
{

    [TestClass]
    public class DivisionByZeroDetectorUnitTest
    {
        //D - Divide
        //DA - DivideAssignment


        //No diagnostics expected to show up
        [TestMethod]
        public async Task NoProgram_NoDiagnostics()
        {
            var test = "";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task D_DivideNumberByTwo_NoDiagnostic()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int y = {|#0: 3 / 2|};
        }
    };
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic is triggered and checked for
        [TestMethod]
        public async Task D_DivideNumberByZero_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int y = {|#0:3 / 0|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            var expectedStandartDiagnostic = new DiagnosticResult(DiagnosticResult.CompilerError("CS0020").Id,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic, expectedStandartDiagnostic);
        }

        [TestMethod]
        public async Task D_DivideNumberByZeroConstant_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            const int x = 0;
            int y = {|#0:3 / x|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            var expectedStandartDiagnostic = new DiagnosticResult(DiagnosticResult.CompilerError("CS0020").Id,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic, expectedStandartDiagnostic);
        }


        [TestMethod]
        public async Task D_DivideVariableByZeroConstant_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            const int x = 0;
            int y = 4;
            y = {|#0:y / 0|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            //standart diagnostic is not triggered
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic);
        }


        [TestMethod]
        public async Task DA_DivideVariableByZeroConstant_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            const int x = 0;
            int y = 4;
            {|#0:y /= 0|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            //standart diagnostic is not triggered
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic);
        }


        [TestMethod]
        public async Task D_DivideNumberBySimpleNumericExpression_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int y = {|#0:3 / (2 - 2)|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            var expectedStandartDiagnostic = new DiagnosticResult(DiagnosticResult.CompilerError("CS0020").Id,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic, expectedStandartDiagnostic);
        }


        [TestMethod]
        public async Task DA_DivideVariableBySimpleNumericExpression_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int y = 4;
            {|#0:y /= (2 - 2)|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic);
        }


        [TestMethod]
        public async Task D_DivideNumberBySimpleConstantExpression_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            const int x = 2;
            int y = {|#0:3 / (x - x)|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            var expectedStandartDiagnostic = new DiagnosticResult(DiagnosticResult.CompilerError("CS0020").Id,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic, expectedStandartDiagnostic);
        }


        [TestMethod]
        public async Task DA_DivideVariableBySimpleConstantExpression_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            const int x = 2;
            int y = 4;
            {|#0:y /= (x - x)|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic);
        }


        //Diagnostic is not triggered when it is expected. Analyzer is still in development

        [TestMethod]
        public async Task D_DivideNumberByZeroVariable_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int x = 0;
            int y = {|#0:3 / x|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            var expectedStandartDiagnostic = new DiagnosticResult(DiagnosticResult.CompilerError("CS0020").Id,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic, expectedStandartDiagnostic);
        }


        [TestMethod]
        public async Task DA_DivideVariableByZeroVariable_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int x = 0;
            int y = 4;
            {|#0:y /= x|};
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic);
        }
    }
}
