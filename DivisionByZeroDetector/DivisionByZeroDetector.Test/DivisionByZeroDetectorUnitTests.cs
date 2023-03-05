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

        // Divide assagnment is the same as simple assignment, so there is only one test for it
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

        //Diagnostic is triggered for x and not triggered for y;

        [TestMethod]
        public async Task D_DivideNumberByZeroVariable_Test1_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int x = 0, y = 1;
            x = {|#0:3 / x|};
            y = 3 / y;
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic);
        }

        [TestMethod]
        public async Task D_DivideNumberByZeroVariable_Test2_DiagnosticIsTriggered()
        {
            var test = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            int x = 1, y = 0;
            x = 0;
            y = 1;
            x = {|#0:3 / x|};
            y = 3 / y;
        }
    };
";

            var expectedDetectorDiagnostic = new DiagnosticResult(DivisionByZeroDetectorAnalyzer.DiagnosticId,
                DiagnosticSeverity.Error).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedDetectorDiagnostic);
        }
                
    }
}
