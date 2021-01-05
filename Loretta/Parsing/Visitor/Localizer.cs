using System;
using System.Collections.Generic;
using System.Linq;
using GParse;
using GParse.Lexing;
using Loretta.Lexing;
using Loretta.Parsing.AST;
using Loretta.Utilities;

namespace Loretta.Parsing.Visitor
{
    /// <summary>
    /// A module that localizes all function calls in a given tree.
    /// </summary>
    public static class Localizer
    {
        private readonly struct LocalizedFunction : IEquatable<LocalizedFunction>
        {
            public LocalizedFunction ( Boolean isMethod, String fullName, String shortName, Expression expression )
            {
                if ( String.IsNullOrWhiteSpace ( fullName ) )
                    throw new ArgumentException ( $"'{nameof ( fullName )}' cannot be null or whitespace.", nameof ( fullName ) );
                if ( String.IsNullOrWhiteSpace ( shortName ) )
                    throw new ArgumentException ( $"'{nameof ( shortName )}' cannot be null or whitespace.", nameof ( shortName ) );

                this.IsMethod = isMethod;
                this.FullName = fullName;
                this.ShortName = shortName;
                this.Expression = expression ?? throw new ArgumentNullException ( nameof ( expression ) );
            }

            public Boolean IsMethod { get; }
            public String FullName { get; }
            public String ShortName { get; }
            public Expression Expression { get; }

            public override Boolean Equals ( Object? obj ) => obj is LocalizedFunction function && this.Equals ( function );
            public Boolean Equals ( LocalizedFunction other ) => this.IsMethod == other.IsMethod && this.FullName == other.FullName && this.ShortName == other.ShortName;
            public override Int32 GetHashCode ( ) => HashCode.Combine ( this.IsMethod, this.FullName, this.ShortName );

            public static Boolean operator == ( LocalizedFunction left, LocalizedFunction right ) => left.Equals ( right );
            public static Boolean operator != ( LocalizedFunction left, LocalizedFunction right ) => !( left == right );
        }

        private static String? NameToString ( Expression expression, String separator )
        {
            return expression switch
            {
                IndexExpression index when NameToFullString ( index.Indexee ) is String indexee => index.Type switch
                {
                    IndexType.Member => String.Concat ( indexee, separator, ( ( IdentifierExpression ) index.Indexer ).Identifier ),
                    IndexType.Method => String.Concat ( indexee, separator, ( ( IdentifierExpression ) index.Indexer ).Identifier ),
                    IndexType.Indexer when index.Indexer.IsConstant
                                           && index.Indexer.ConstantValue is String indexer
                                           && StringUtils.IsIdentifier ( false, indexer ) => String.Concat ( indexee, separator, indexer ),
                    _ => null
                },
                IdentifierExpression identifier => identifier.Identifier,
                _ => null,
            };
        }

        private static Scope? GetScope ( Expression expression ) =>
            expression switch
            {
                IndexExpression index => GetScope ( index.Indexee ),
                IdentifierExpression identifier => identifier.Variable?.ParentScope,
                _ => null
            };

        private static String? NameToFullString ( Expression expression ) => NameToString ( expression, "." );

        private static String? NameToShortString ( Expression expression ) => NameToString ( expression, "_" );

        private static LocalizedFunction? NameToLocalizedFunction ( Expression expression )
        {
            var fullName = NameToFullString ( expression );
            var shortName = NameToShortString ( expression );
            if ( fullName is null || shortName is null )
                return null;

            return expression switch
            {
                IndexExpression index => new LocalizedFunction ( index.Type == IndexType.Method, fullName, shortName, memberifyIndex ( index ) ),
                IdentifierExpression identifier => new LocalizedFunction ( false, fullName, shortName, identifier ),
                _ => null
            };

            static IndexExpression memberifyIndex ( IndexExpression expression ) =>
                new ( expression.Indexee, new Token<LuaTokenType> ( ".", ".", ".", LuaTokenType.Dot, SourceRange.Zero ), ( IdentifierExpression ) expression.Indexer );
        }

        private class Walker : TreeWalkerBase
        {
            private readonly Scope _rootScope;

            public Walker ( Scope rootScope )
            {
                this.Localizations = new Dictionary<Expression, LocalizedFunction> ( );
                this._rootScope = rootScope;
            }

            public IDictionary<Expression, LocalizedFunction> Localizations { get; }

            public override void VisitFunctionCall ( FunctionCallExpression node )
            {
                Scope? scope = GetScope ( node.Function );
                if ( scope is not null && scope == this._rootScope )
                {
                    if ( NameToLocalizedFunction ( node.Function ) is LocalizedFunction localized )
                        this.Localizations.Add ( node.Function, localized );
                }
                base.VisitFunctionCall ( node );
            }
        }

        private class Folder : TreeFolderBase
        {
            private readonly Scope rootScope;
            private readonly IDictionary<Expression, LocalizedFunction> localizedFunctions;

            public Folder ( Scope rootScope, IDictionary<Expression, LocalizedFunction> localizedFunctions )
            {
                this.rootScope = rootScope ?? throw new ArgumentNullException ( nameof ( rootScope ) );
                this.localizedFunctions = localizedFunctions ?? throw new ArgumentNullException ( nameof ( localizedFunctions ) );
            }

            public override LuaASTNode VisitFunctionCall ( FunctionCallExpression node )
            {
                if ( this.localizedFunctions.TryGetValue ( node.Function, out LocalizedFunction localized ) )
                {
                    node = ( FunctionCallExpression ) base.VisitFunctionCall ( node );
                    if ( localized.IsMethod )
                    {
                        Expression? indexee = ( ( IndexExpression ) node.Function ).Indexee;
                        return new FunctionCallExpression (
                            ASTNodeFactory.Identifier ( localized.ShortName ),
                            node.Tokens.First ( ),
                            new[] { indexee }.Concat ( node.Arguments ),
                            new[] { new Token<LuaTokenType> ( ",", ",", ",", LuaTokenType.Comma, SourceRange.Zero ) }.Concat ( node.Tokens.Skip ( 1 ).SkipLast ( 1 ) ),
                            node.Tokens.Last ( ) );
                    }

                    return new FunctionCallExpression (
                        ASTNodeFactory.Identifier ( localized.ShortName ),
                        node.Tokens.First ( ),
                        node.Arguments,
                        node.Tokens.Skip ( 1 ).SkipLast ( 1 ),
                        node.Tokens.Last ( ) );
                }
                return base.VisitFunctionCall ( node );
            }

            public StatementList VisitRoot ( StatementList statementList )
            {
                if ( statementList is null )
                    throw new ArgumentNullException ( nameof ( statementList ) );

                var locals = new List<LocalVariableDeclarationStatement> ( );
                foreach ( LocalizedFunction localized in this.localizedFunctions.Values.Distinct ( ) )
                {
                    var variable = new Variable ( localized.ShortName, this.rootScope );
                    this.rootScope.AddVariable ( variable );

                    locals.Add ( new LocalVariableDeclarationStatement (
                        new Token<LuaTokenType> ( "local", "local", "local", LuaTokenType.Keyword, SourceRange.Zero ),
                        new[] { new IdentifierExpression ( TokenFactory.Identifier ( localized.ShortName ), variable ) },
                        Enumerable.Empty<Token<LuaTokenType>> ( ),
                        new Token<LuaTokenType> ( ",", ",", ",", LuaTokenType.Comma, SourceRange.Zero ),
                        new[] { localized.Expression },
                        Enumerable.Empty<Token<LuaTokenType>> ( ) ) );
                }

                var list = ( StatementList ) statementList.Accept ( this );
                return new StatementList ( list.Scope, locals.Concat ( list.Body ) );
            }
        }

        public static StatementList Localize ( StatementList statementList, Scope rootScope )
        {
            var walker = new Walker ( rootScope );
            statementList.Accept ( walker );
            var folder = new Folder ( rootScope, walker.Localizations );
            return folder.VisitRoot ( statementList );
        }
    }
}
