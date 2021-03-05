﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Loretta.CodeAnalysis.PooledObjects;

namespace Loretta.CodeAnalysis.Lua
{
    public sealed class LuaParseOptions : ParseOptions, IEquatable<LuaParseOptions?>
    {
        private ImmutableDictionary<string, string> _features;

        /// <summary>
        /// Initializes a new set of parse options.
        /// </summary>
        /// <param name="syntaxOptions"></param>
        public LuaParseOptions(LuaSyntaxOptions syntaxOptions)
            : base(DocumentationMode.Parse)
        {
            _features = ImmutableDictionary<string, string>.Empty;
            SyntaxOptions = syntaxOptions;
        }


        internal LuaParseOptions(LuaParseOptions other) : this(other.SyntaxOptions)
        {
            _features = other._features;
        }

        /// <summary>
        /// The <see cref="LuaSyntaxOptions"/> to use when parsing.
        /// </summary>
        public LuaSyntaxOptions SyntaxOptions { get; private set; }

        /// <summary>
        /// <b><see cref="DocumentationMode"/> does nothing currently.</b>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new DocumentationMode DocumentationMode
        {
            get => base.DocumentationMode;
            private set => base.DocumentationMode = value;
        }

        /// <inheritdoc/>
        public override string Language => LanguageNames.Lua;

        /// <summary>
        /// <b>The features flag don't do anything currently.</b>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IReadOnlyDictionary<string, string> Features => _features;

        public override IEnumerable<string> PreprocessorSymbolNames => throw new NotImplementedException();

        /// <summary>
        /// <inheritdoc cref="ParseOptions.WithDocumentationMode(DocumentationMode)"/>.
        /// <b><see cref="CodeAnalysis.DocumentationMode"/> does nothing currently.</b>
        /// </summary>
        /// <param name="documentationMode"><inheritdoc cref="ParseOptions.WithDocumentationMode(DocumentationMode)"/>.</param>
        /// <returns><inheritdoc cref="ParseOptions.WithDocumentationMode(DocumentationMode)"/>.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new LuaParseOptions WithDocumentationMode(DocumentationMode documentationMode)
        {
            if (DocumentationMode != documentationMode)
                return new LuaParseOptions(this) { DocumentationMode = documentationMode };
            return this;
        }

        /// <summary>
        /// <inheritdoc cref="ParseOptions.WithFeatures(IEnumerable{KeyValuePair{string, string}})"/>
        /// <b>Feature flags don't do anything currently.</b>
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new LuaParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>>? features)
            => new LuaParseOptions(this) { _features = features?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase) ?? ImmutableDictionary<string, string>.Empty };


        /// <summary>
        /// Creates a new instance with the syntax options replaced by the provided ones.
        /// </summary>
        /// <param name="syntaxOptions"></param>
        /// <returns></returns>
        public LuaParseOptions WithSyntaxOptions(LuaSyntaxOptions syntaxOptions)
        {
            if (!SyntaxOptions.Equals(syntaxOptions))
                return new LuaParseOptions(this) { SyntaxOptions = syntaxOptions };
            return this;
        }

        /// <inheritdoc/>
        protected override ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode)
            => WithDocumentationMode(documentationMode);

        /// <inheritdoc/>
        protected override ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features)
            => WithFeatures(features);

        internal override void ValidateOptions(ArrayBuilder<Diagnostic> builder)
            => ValidateOptions(builder, MessageProvider.Instance);

        /// <inheritdoc/>
        public bool Equals(LuaParseOptions? other) =>
            ReferenceEquals(this, other)
            || EqualsHelper(this)
            || (other is not null && SyntaxOptions.Equals(other.SyntaxOptions));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as LuaParseOptions);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(GetHashCodeHelper(), SyntaxOptions);
    }
}