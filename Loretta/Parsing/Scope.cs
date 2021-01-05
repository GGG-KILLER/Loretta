using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Loretta.Lexing;
using LuaToken = GParse.Lexing.Token<Loretta.Lexing.LuaTokenType>;

namespace Loretta.Parsing
{
    /// <summary>
    /// Represents a scope.
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// The variable search mode.
        /// </summary>
        public enum FindMode
        {
            /// <summary>
            /// Only checks the current scope
            /// </summary>
            CheckSelf,

            /// <summary>
            /// Checks parents until we hit a function scope
            /// </summary>
            CheckFunctionScope,

            /// <summary>
            /// Checks all parents
            /// </summary>
            CheckParents,
        }

        /// <summary>
        /// The parent scope.
        /// </summary>
        public Scope? Parent { get; }

        /// <summary>
        /// The variables registered in this scope.
        /// </summary>
        private readonly Dictionary<String, Variable> _variables = new Dictionary<String, Variable> ( );

        /// <summary>
        /// The goto labels registered in this scope.
        /// </summary>
        private readonly Dictionary<String, GotoLabel> _gotoLabels = new Dictionary<String, GotoLabel> ( );

        /// <inheritdoc cref="_variables" />
        // Yes, you can technically edit the dictionary like this, but when one wants to abuse an
        // API they will find a way to do it.
        public IEnumerable<Variable> Variables => this._variables.Values;

        /// <inheritdoc cref="_gotoLabels" />
        // Yes, you can technically edit the dictionary like this, but when one wants to abuse an
        // API they will find a way to do it.
        public IEnumerable<GotoLabel> GotoLabels => this._gotoLabels.Values;

        /// <summary>
        /// Whether this scope is a function's.
        /// </summary>
        public Boolean IsFunctionScope { get; }

        /// <summary>
        /// Initializes a new scope.
        /// </summary>
        /// <param name="isFunctionScope"><see cref="IsFunctionScope" /></param>
        public Scope ( Boolean isFunctionScope )
        {
            this.Parent = null;
            this.IsFunctionScope = isFunctionScope;
        }

        /// <summary>
        /// Initializes a new scope.
        /// </summary>
        /// <param name="parent"><see cref="Parent" /></param>
        /// <param name="isFunctionScope"><see cref="IsFunctionScope" /></param>
        public Scope ( Scope parent, Boolean isFunctionScope )
        {
            this.Parent = parent;
            this.IsFunctionScope = isFunctionScope;
        }

        #region TryFindVariable

        /// <summary>
        /// Attempts to find a variable.
        /// </summary>
        /// <param name="identifier">The identifier token.</param>
        /// <param name="findMode">The variable search mode.</param>
        /// <param name="variable">The variable found (if any).</param>
        /// <returns>Whether the variable was found.</returns>
        public Boolean TryFindVariable ( LuaToken identifier, FindMode findMode, [NotNullWhen ( true )] out Variable? variable )
        {
            if ( identifier.Type != LuaTokenType.Identifier || !( identifier.Value is String value ) )
                throw new ArgumentException ( "Token is not an identifier", nameof ( identifier ) );

            return this.TryFindVariable ( value, findMode, out variable );
        }

        /// <inheritdoc cref="TryFindVariable(LuaToken, FindMode, out Variable?)" />
        public Boolean TryFindVariable ( String identifier, FindMode findMode, [NotNullWhen ( true )] out Variable? variable )
        {
            if ( this._variables.TryGetValue ( identifier, out variable ) )
                return true;

            if ( ( ( findMode == FindMode.CheckFunctionScope && !this.IsFunctionScope ) || findMode == FindMode.CheckParents ) && this.Parent != null )
                return this.Parent.TryFindVariable ( identifier, findMode, out variable );

            variable = null;
            return false;
        }

        #endregion TryFindVariable

        #region GetVariable

        /// <summary>
        /// Finds or creates the variable with the provided name.
        /// </summary>
        /// <param name="identifier">The identifier containing the name.</param>
        /// <param name="findMode">The search mode.</param>
        /// <returns>The found or created variable.</returns>
        public Variable GetVariable ( LuaToken identifier, FindMode findMode )
        {
            if ( identifier.Type != LuaTokenType.Identifier || !( identifier.Value is String value ) )
                throw new ArgumentException ( "Token is not an identifier", nameof ( identifier ) );
            return this.GetVariable ( value, findMode );
        }

        /// <inheritdoc cref="GetVariable(LuaToken, FindMode)" />
        public Variable GetVariable ( String identifier, FindMode findMode )
        {
            if ( !this.TryFindVariable ( identifier, findMode, out Variable? variable ) )
            {
                variable = new Variable ( identifier, this );
                this._variables.Add ( identifier, variable );
            }

            return variable;
        }

        #endregion GetVariable

        /// <summary>
        /// Adds a variable to this scope.
        /// </summary>
        /// <param name="variable"></param>
        public void AddVariable ( Variable variable ) =>
            this._variables.Add ( variable.Identifier, variable );

        #region FindLabel

        /// <summary>
        /// Attempts to find a goto label. Returns null if none is found.
        /// </summary>
        /// <param name="labelIdentifier">The label's identifier.</param>
        /// <param name="findMode">The label search mode.</param>
        /// <returns>The label or null if not found.</returns>
        public GotoLabel? FindLabel ( LuaToken labelIdentifier, FindMode findMode )
        {
            if ( labelIdentifier.Type != LuaTokenType.Identifier || !( labelIdentifier.Value is String value ) )
                throw new ArgumentException ( "Token is not an identifier", nameof ( labelIdentifier ) );

            return this.FindLabel ( value, findMode );
        }

        /// <inheritdoc cref="FindLabel(LuaToken, FindMode)" />
        public GotoLabel? FindLabel ( String labelIdentifier, FindMode findMode )
        {
            if ( !this.TryFindLabel ( labelIdentifier, findMode, out GotoLabel? label ) )
                return null;
            return label;
        }

        #endregion FindLabel

        #region TryFindLabel

        /// <summary>
        /// Attempts to find a goto label with the provided identifier.
        /// </summary>
        /// <param name="labelIdentifier">The label's identifier.</param>
        /// <param name="findMode">The label search mode.</param>
        /// <param name="label">The label, if found.</param>
        /// <returns>Whether the label was found.</returns>
        public Boolean TryFindLabel ( LuaToken labelIdentifier, FindMode findMode, [NotNullWhen ( true )] out GotoLabel? label )
        {
            if ( labelIdentifier.Type != LuaTokenType.Identifier || !( labelIdentifier.Value is String value ) )
                throw new ArgumentException ( "Token is not am identifier", nameof ( labelIdentifier ) );

            return this.TryFindLabel ( value, findMode, out label );
        }

        /// <inheritdoc cref="TryFindLabel(LuaToken, FindMode, out GotoLabel?)" />
        public Boolean TryFindLabel ( String labelIdentifier, FindMode findMode, [NotNullWhen ( true )] out GotoLabel? label )
        {
            if ( this._gotoLabels.TryGetValue ( labelIdentifier, out label ) )
                return true;

            if ( ( ( findMode == FindMode.CheckFunctionScope && !this.IsFunctionScope ) || findMode == FindMode.CheckParents ) && this.Parent != null )
                return this.Parent.TryFindLabel ( labelIdentifier, findMode, out label );

            label = null;
            return false;
        }

        #endregion TryFindLabel

        #region GetLabel

        /// <summary>
        /// Finds or creates a label.
        /// </summary>
        /// <param name="labelIdentifier">The label's identifier.</param>
        /// <param name="findMode">The label search mode.</param>
        /// <returns>The found or created label.</returns>
        public GotoLabel GetLabel ( LuaToken labelIdentifier, FindMode findMode )
        {
            if ( labelIdentifier.Type != LuaTokenType.Identifier || !( labelIdentifier.Value is String value ) )
                throw new ArgumentException ( "Token is not an identifier", nameof ( labelIdentifier ) );
            return this.GetLabel ( value, findMode );
        }

        /// <inheritdoc cref="GetLabel(LuaToken, FindMode)" />
        public GotoLabel GetLabel ( String labelIdentifier, FindMode findMode )
        {
            if ( !this.TryFindLabel ( labelIdentifier, findMode, out GotoLabel? label ) )
            {
                label = new GotoLabel ( this, labelIdentifier );
                this._gotoLabels.Add ( labelIdentifier, label );
            }

            return label;
        }

        #endregion GetLabel
    }
}