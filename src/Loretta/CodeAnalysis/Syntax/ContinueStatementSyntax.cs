﻿using Tsu;

namespace Loretta.CodeAnalysis.Syntax
{
    /// <summary>
    /// Represents a continue statement.
    /// </summary>
    public sealed partial class ContinueStatementSyntax : StatementSyntax
    {
        internal ContinueStatementSyntax ( SyntaxToken continueKeyword, Option<SyntaxToken> semicolonToken )
            : base ( semicolonToken )
        {
            this.ContinueKeyword = continueKeyword;
        }

        /// <inheritdoc/>
        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;

        /// <summary>
        /// The 'continue' keyword.
        /// </summary>
        public SyntaxToken ContinueKeyword { get; }
    }
}