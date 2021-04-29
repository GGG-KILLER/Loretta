using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Loretta.CodeAnalysis.Lua.Test.Utilities;
using Loretta.CodeAnalysis.Text;
using Loretta.Test.Utilities;
using Xunit;

namespace Loretta.CodeAnalysis.Lua.Syntax.UnitTests.Diagnostics
{
    public partial class DiagnosticsTests : LuaTestBase
    {
        [Fact]
        public void AllDiagnosticsHaveMessages()
        {
            var excludedErrorCodes = new[]
            {
                ErrorCode.Void,
                ErrorCode.Unknown,
            };

            foreach (ErrorCode code in Enum.GetValues(typeof(ErrorCode)))
            {
                if (excludedErrorCodes.Contains(code))
                    continue;

                var message = ErrorFacts.GetMessage(code, CultureInfo.InvariantCulture);
                Assert.False(string.IsNullOrWhiteSpace(message));
            }
        }

        [Fact]
        public void ErrorCodeHasNoDuplicates()
        {
            var values = (ErrorCode[]) Enum.GetValues(typeof(ErrorCode));
            var set = new HashSet<ErrorCode>();
            foreach (var value in values)
                Assert.True(set.Add(value), $"{value} has a duplicate.");
        }

        [Fact]
        public void TestDiagnostic()
        {
            var provider = new MockMessageProvider();
            SyntaxTree syntaxTree = new MockSyntaxTree();
            CultureInfo englishCulture = CultureHelpers.EnglishCulture;

            DiagnosticInfo di1 = new DiagnosticInfo(provider, 1);
            Assert.Equal(1, di1.Code);
            Assert.Equal(DiagnosticSeverity.Error, di1.Severity);
            Assert.Equal("MOCK0001", di1.MessageIdentifier);
            Assert.Equal("The first error", di1.GetMessage(englishCulture));

            DiagnosticInfo di2 = new DiagnosticInfo(provider, 1002, "Elvis", "Mort");
            Assert.Equal(1002, di2.Code);
            Assert.Equal(DiagnosticSeverity.Warning, di2.Severity);
            Assert.Equal("MOCK1002", di2.MessageIdentifier);
            Assert.Equal("The second warning about Elvis and Mort", di2.GetMessage(englishCulture));

            Location l1 = new SourceLocation(syntaxTree, new TextSpan(5, 8));
            var d1 = new LuaDiagnostic(di2, l1);
            Assert.Equal(l1, d1.Location);
            Assert.Same(syntaxTree, d1.Location.SourceTree);
            Assert.Equal(new TextSpan(5, 8), d1.Location.SourceSpan);
            Assert.Empty(d1.AdditionalLocations);
            Assert.Same(di2, d1.Info);
        }
    }
}
