using System.Globalization;
using Loretta.Test.Utilities;

namespace Loretta.CodeAnalysis.Lua.Syntax.UnitTests.Diagnostics
{
    public partial class DiagnosticsTests
    {
        private class MockMessageProvider : TestMessageProvider
        {
            public override DiagnosticSeverity GetSeverity(int code) =>
                code >= 1000 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;

            public override string? LoadMessage(int code, CultureInfo language) =>
                code switch
                {
                    1 => "The first error",
                    2 => "The second error is associated with symbol {0}",
                    1001 => "The first warning",
                    1002 => "The second warning about {0} and {1}",
                    _ => string.Empty
                };

            public override LocalizableString GetDescription(int code) =>
                string.Empty;
            public override LocalizableString GetTitle(int code) =>
                string.Empty;
            public override LocalizableString GetMessageFormat(int code) =>
                string.Empty;
            public override string GetHelpLink(int code) =>
                string.Empty;
            public override string GetCategory(int code) =>
                string.Empty;
            public override string CodePrefix =>
                "MOCK";
            public override int GetWarningLevel(int code) =>
                code >= 1000 ? code % 4 + 1 : 0;
        }
    }
}
