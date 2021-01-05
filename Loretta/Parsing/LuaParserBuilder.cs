using System;
using System.Collections.Generic;
using GParse;
using GParse.Lexing;
using GParse.Parsing;
using Loretta.Lexing;
using Loretta.Parsing.AST;
using Loretta.Parsing.Modules;
using LuaToken = GParse.Lexing.Token<Loretta.Lexing.LuaTokenType>;

namespace Loretta.Parsing
{
    /// <summary>
    /// The lua parser builder.
    /// </summary>
    public class LuaParserBuilder : PrattParserBuilder<LuaTokenType, Expression>
    {
        /// <summary>
        /// The lua options to be used by this builder and the parsers it builds.
        /// </summary>
        public LuaOptions LuaOptions { get; }
        public IEnumerable<String> Globals { get; }

        private static Boolean StringExpressionFactory ( LuaToken token, out Expression expression )
        {
            expression = new StringExpression ( token );
            return true;
        }

        private static Boolean NumberExpressionFactory ( LuaToken token, out Expression expression )
        {
            expression = new NumberExpression ( token );
            return true;
        }

        private static Boolean NilExpressionFactory ( LuaToken token, out Expression expression )
        {
            expression = new NilExpression ( token );
            return true;
        }

        private static Boolean BooleanExpressionFactory ( LuaToken token, out Expression expression )
        {
            expression = new BooleanExpression ( token, ( Boolean ) token.Value! );
            return true;
        }

        private static Boolean VarArgExpressionFactory ( LuaToken token, out Expression expression )
        {
            expression = new VarArgExpression ( token );
            return true;
        }

        /// <summary>
        /// Initializes a new lua parser builder.
        /// </summary>
        /// <param name="luaOptions">The lua options to be used.</param>
        public LuaParserBuilder ( LuaOptions luaOptions, IEnumerable<String> globals )
        {
            this.LuaOptions = luaOptions ?? throw new ArgumentNullException ( nameof ( luaOptions ) );
            this.Globals = globals ?? throw new ArgumentNullException ( nameof ( globals ) );

            #region Value Expressions

            this.RegisterLiteral ( LuaTokenType.String, StringExpressionFactory );
            this.RegisterLiteral ( LuaTokenType.LongString, StringExpressionFactory );
            this.RegisterLiteral ( LuaTokenType.Number, NumberExpressionFactory );
            this.RegisterLiteral ( LuaTokenType.Nil, NilExpressionFactory );
            this.RegisterLiteral ( LuaTokenType.Boolean, "true", BooleanExpressionFactory );
            this.RegisterLiteral ( LuaTokenType.Boolean, "false", BooleanExpressionFactory );
            this.RegisterLiteral ( LuaTokenType.VarArg, VarArgExpressionFactory );

            IdentifierParserModule.Register ( this );
            AnonymousFunctionExpressionParserModule.Register ( this );
            GroupedExpressionParserModule.Register ( this );
            TableConstructorExpressionParserModule.Register ( this );

            #endregion Value Expressions

            #region Operators

            this.AddBinaryOperator ( 1, false, "or" );

            this.AddBinaryOperator ( 2, false, "and" );

            this.AddBinaryOperator ( 3, false, "==" );
            this.AddBinaryOperator ( 3, false, "~=" );
            this.AddBinaryOperator ( 3, false, "<" );
            this.AddBinaryOperator ( 3, false, "<=" );
            this.AddBinaryOperator ( 3, false, ">" );
            this.AddBinaryOperator ( 3, false, ">=" );

            this.AddBinaryOperator ( 4, true, ".." );

            this.AddBinaryOperator ( 5, false, "+" );
            this.AddBinaryOperator ( 5, false, "-" );

            this.AddBinaryOperator ( 6, false, "*" );
            this.AddBinaryOperator ( 6, false, "/" );
            this.AddBinaryOperator ( 6, false, "%" );

            this.AddPrefixOperator ( 7, "not" );
            this.AddPrefixOperator ( 7, "#" );
            this.AddPrefixOperator ( 7, "-" );

            this.AddBinaryOperator ( 8, true, "^" );

            FunctionCallExpressionParserModule.Register ( this, 9 );
            IndexExpressionParserModule.Register ( this, 9 );

            #endregion Operators
        }

        #region Operator Management

        /// <summary>
        /// The factory method for <see cref="BinaryOperationExpression" /> nodes.
        /// </summary>
        /// <param name="left">The operand on the left side.</param>
        /// <param name="op">The operator.</param>
        /// <param name="right">The operand on the right side.</param>
        /// <param name="expression">The created <see cref="BinaryOperationExpression" />.</param>
        /// <returns><see langword="true" /></returns>
        protected virtual Boolean BinaryOperatorFactory ( Expression left, LuaToken op, Expression right, out Expression expression )
        {
            expression = new BinaryOperationExpression ( left, op, right );
            return true;
        }

        /// <summary>
        /// The factory method for prefix <see cref="UnaryOperationExpression" /> nodes.
        /// </summary>
        /// <param name="op">The operator.</param>
        /// <param name="left">The operand.</param>
        /// <param name="expression">The created <see cref="UnaryOperationExpression" /></param>
        /// <returns><see langword="true" /></returns>
        protected virtual Boolean PrefixOperatorFactory ( LuaToken op, Expression left, out Expression expression )
        {
            expression = new UnaryOperationExpression ( UnaryOperationFix.Prefix, op, left );
            return true;
        }

        /// <summary>
        /// The factory method for postfix <see cref="UnaryOperationExpression" /> nodes.
        /// </summary>
        /// <param name="right">The operator.</param>
        /// <param name="op">The operand.</param>
        /// <param name="expression">The created <see cref="UnaryOperationExpression" />.</param>
        /// <returns><see langword="true" /></returns>
        protected virtual Boolean PostfixOperatorFactory ( Expression right, LuaToken op, out Expression expression )
        {
            expression = new UnaryOperationExpression ( UnaryOperationFix.Postfix, op, right );
            return true;
        }

        /// <summary>
        /// Adds a binary operator to this builder.
        /// </summary>
        /// <param name="precedence">The operator's precedence.</param>
        /// <param name="isRightAssociative">Whether the operator is right-associative.</param>
        /// <param name="op">The operator token id.</param>
        protected virtual void AddBinaryOperator ( Int32 precedence, Boolean isRightAssociative, String op ) =>
            this.RegisterSingleTokenInfixOperator (
                LuaTokenType.Operator,
                op,
                precedence,
                isRightAssociative,
                this.BinaryOperatorFactory );

        /// <summary>
        /// Adds a prefix unary operator to this builder.
        /// </summary>
        /// <param name="precedence">The operator's precedence.</param>
        /// <param name="op">The operator token id.</param>
        protected virtual void AddPrefixOperator ( Int32 precedence, String op ) =>
            this.RegisterSingleTokenPrefixOperator ( LuaTokenType.Operator, op, precedence, this.PrefixOperatorFactory );

        /// <summary>
        /// Adds a postfix unary operator to this builder.
        /// </summary>
        /// <param name="precedence">The operator's precedence.</param>
        /// <param name="op">The operator token id.</param>
        protected virtual void AddPostfixOperator ( Int32 precedence, String op ) =>
            this.RegisterSingleTokenPostfixOperator ( LuaTokenType.Operator, op, precedence, this.PostfixOperatorFactory );

        #endregion Operator Management

        /// <summary>
        /// Creates a <see cref="LuaParser" /> from the provided reader and diagnostic emitter.
        /// </summary>
        /// <param name="reader">The token reader to use.</param>
        /// <param name="diagnosticEmmiter">The diagnostic emitter.</param>
        /// <param name="rootScope"></param>
        /// <returns></returns>
        public LuaParser CreateParser ( ITokenReader<LuaTokenType> reader, IProgress<Diagnostic> diagnosticEmmiter, out Scope rootScope )
        {
            var parser = new LuaParser ( this.LuaOptions, reader, this.prefixModuleTree, this.infixModuleTree, diagnosticEmmiter );
            rootScope = parser.EnterScope ( true );
            foreach ( var global in this.Globals ) rootScope.AddVariable ( new Variable ( global, rootScope ) );
            return parser;
        }
    }
}