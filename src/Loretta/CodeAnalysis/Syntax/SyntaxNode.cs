﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Loretta.CodeAnalysis.Text;

namespace Loretta.CodeAnalysis.Syntax
{
    /// <summary>
    /// The base class for all syntax nodes.
    /// </summary>
    public abstract class SyntaxNode
    {
        private protected SyntaxNode ( SyntaxTree syntaxTree )
        {
            this.SyntaxTree = syntaxTree;
        }

        /// <summary>
        /// The <see cref="Syntax.SyntaxTree"/> this node belongs to.
        /// </summary>
        public SyntaxTree SyntaxTree { get; }

        /// <summary>
        /// This node's parent.
        /// </summary>
        public SyntaxNode? Parent => this.SyntaxTree.GetParent ( this );

        /// <summary>
        /// This node's kind.
        /// </summary>
        public abstract SyntaxKind Kind { get; }

        /// <summary>
        /// The span of this node.
        /// </summary>
        public virtual TextSpan Span
        {
            get
            {
                TextSpan first = this.GetChildren ( ).First ( ).Span;
                TextSpan last = this.GetChildren ( ).Last ( ).Span;
                return TextSpan.FromBounds ( first.Start, last.End );
            }
        }

        /// <summary>
        /// The span of this node including trivia.
        /// </summary>
        public virtual TextSpan FullSpan
        {
            get
            {
                TextSpan first = this.GetChildren ( ).First ( ).FullSpan;
                TextSpan last = this.GetChildren ( ).Last ( ).FullSpan;
                return TextSpan.FromBounds ( first.Start, last.End );
            }
        }

        /// <summary>
        /// This node's location.
        /// </summary>
        public TextLocation Location => new ( this.SyntaxTree.Text, this.Span );

        /// <summary>
        /// This node's diagnostics.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Throw when the node is a <see cref="SyntaxToken"/> and <see cref="SyntaxToken.IsMissing"/> is <see langword="true"/>.
        /// This is because since missing tokens are generated by the compiler, they (in theory) shouldn't have any diagnostics
        /// being reported for them.
        /// </exception>
        public IEnumerable<Diagnostic> Diagnostics
        {
            get
            {
                if ( this is SyntaxToken token && token.IsMissing )
                    throw new InvalidOperationException ( "Cannot list the diagnostics of a missing node." );

                return this.SyntaxTree.Diagnostics.Where ( diag => diag.Location.Span.OverlapsWith ( this.Span ) );
            }
        }

        /// <summary>
        /// Returns this node and its ancestors in order.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SyntaxNode> AncestorsAndSelf ( )
        {
            SyntaxNode? node = this;
            while ( node != null )
            {
                yield return node;
                node = node.Parent;
            }
        }

        /// <summary>
        /// Returns this node ancestors in order.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SyntaxNode> Ancestors ( )
        {
            SyntaxNode? node = this.Parent;
            while ( node != null )
            {
                yield return node;
                node = node.Parent;
            }
        }

        /// <summary>
        /// Retrieves all immediate children from this node.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<SyntaxNode> GetChildren ( );

        /// <summary>
        /// Returns the last token of the tree rooted by this node.
        /// </summary>
        /// <returns></returns>
        public virtual SyntaxToken GetLastToken ( )
        {
            if ( this is SyntaxToken token )
                return token;

            return this.GetChildren ( ).Last ( ).GetLastToken ( );
        }

        /// <summary>
        /// Pretty prints a tree to the console.
        /// </summary>
        /// <param name="writer"></param>
        public void PrettyPrintTo ( TextWriter writer ) =>
            PrettyPrint ( writer, this );

        private static void PrettyPrint ( TextWriter writer, SyntaxNode node, String indent = "", Boolean isLast = true )
        {
            var isToConsole = writer == Console.Out;
            var token = node as SyntaxToken;

            if ( token != null )
            {
                foreach ( SyntaxTrivia trivia in token.LeadingTrivia )
                {
                    if ( isToConsole )
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                    writer.Write ( indent );
                    writer.Write ( "├──" );

                    if ( isToConsole )
                        Console.ForegroundColor = ConsoleColor.DarkGreen;

                    writer.WriteLine ( $"L: {trivia.Kind}" );
                }
            }

            var hasTrailingTrivia = token != null && token.TrailingTrivia.Any ( );
            var tokenMarker = !hasTrailingTrivia && isLast ? "└──" : "├──";

            if ( isToConsole )
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write ( indent );
            writer.Write ( tokenMarker );

            if ( isToConsole )
                Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;

            writer.Write ( node.Kind );

            if ( token != null && token.Value.IsSome )
            {
                writer.Write ( " " );
                writer.Write ( token.Value.Value ?? "null" );
            }

            if ( isToConsole )
                Console.ResetColor ( );

            writer.WriteLine ( );

            if ( token != null )
            {
                foreach ( SyntaxTrivia trivia in token.TrailingTrivia )
                {
                    var isLastTrailingTrivia = trivia == token.TrailingTrivia.Last ( );
                    var triviaMarker = isLast && isLastTrailingTrivia ? "└──" : "├──";

                    if ( isToConsole )
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                    writer.Write ( indent );
                    writer.Write ( triviaMarker );

                    if ( isToConsole )
                        Console.ForegroundColor = ConsoleColor.DarkGreen;

                    writer.WriteLine ( $"T: {trivia.Kind}" );
                }
            }

            indent += isLast ? "   " : "│  ";

            SyntaxNode? lastChild = node.GetChildren ( ).LastOrDefault ( );

            foreach ( SyntaxNode? child in node.GetChildren ( ) )
                PrettyPrint ( writer, child, indent, child == lastChild );
        }

        /// <summary>
        /// Pretty-prints a tree of the syntax node.
        /// </summary>
        /// <returns></returns>
        public override String ToString ( )
        {
            using var writer = new StringWriter ( );
            this.PrettyPrintTo ( writer );
            return writer.ToString ( );
        }
    }
}