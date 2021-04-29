using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Loretta.CodeAnalysis.Text;

namespace Loretta.CodeAnalysis.Lua.Syntax.UnitTests.Diagnostics
{
    public partial class DiagnosticsTests
    {
        private class MockSyntaxTree : LuaSyntaxTree
        {
            public override string FilePath =>
                string.Empty;
            public override SourceText GetText(CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public override bool TryGetText([NotNullWhen(true)] out SourceText? text) =>
                throw new NotImplementedException();
            public override Encoding? Encoding =>
                throw new NotImplementedException();
            public override int Length =>
                throw new NotImplementedException();
            public override LuaParseOptions Options =>
                throw new NotImplementedException();
            public override LuaSyntaxNode GetRoot(CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public override bool TryGetRoot([NotNullWhen(true)] out LuaSyntaxNode? root) =>
                throw new NotImplementedException();
            public override SyntaxReference GetReference(SyntaxNode node) =>
                throw new NotImplementedException();
            public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options) =>
                throw new NotImplementedException();
            public override SyntaxTree WithFilePath(string path) =>
                throw new NotImplementedException();
            public override bool HasCompilationUnitRoot =>
                throw new NotImplementedException();
        }
    }
}
