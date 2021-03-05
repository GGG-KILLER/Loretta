﻿using Loretta.Utilities;

namespace Loretta.CodeAnalysis.Lua.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxIdentifierWithTrivia : SyntaxIdentifierExtended
        {
            static SyntaxIdentifierWithTrivia()
            {
                ObjectBinder.RegisterTypeReader(typeof(SyntaxIdentifierWithTrivia), r => new SyntaxIdentifierWithTrivia(r));
            }

            protected readonly GreenNode? _leading;
            protected readonly GreenNode? _trailing;

            internal SyntaxIdentifierWithTrivia(
                SyntaxKind contextualKind,
                string text,
                string valueText,
                GreenNode? leading,
                GreenNode? trailing)
                : base(contextualKind, text, valueText)
            {
                if (leading is not null)
                {
                    AdjustFlagsAndWidth(leading);
                    _leading = leading;
                }
                if (trailing is not null)
                {
                    AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrivia(
                SyntaxKind contextualKind,
                string text,
                string valueText,
                GreenNode? leading,
                GreenNode? trailing,
                DiagnosticInfo[]? diagnostics,
                SyntaxAnnotation[]? annotations)
                : base(contextualKind, text, valueText, diagnostics, annotations)
            {
                if (leading is not null)
                {
                    AdjustFlagsAndWidth(leading);
                    _leading = leading;
                }
                if (trailing is not null)
                {
                    AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrivia(ObjectReader reader)
                : base(reader)
            {
                var leading = (GreenNode?) reader.ReadValue();
                if (leading is not null)
                {
                    AdjustFlagsAndWidth(leading);
                    _leading = leading;
                }
                var trailing = (GreenNode?) reader.ReadValue();
                if (trailing is not null)
                {
                    AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
                }
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(_leading);
                writer.WriteValue(_trailing);
            }

            public override GreenNode? GetLeadingTrivia() => _leading;

            public override GreenNode? GetTrailingTrivia() => _trailing;

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode? trivia)
                => new SyntaxIdentifierWithTrivia(_contextualKind, _name, _valueText, trivia, _trailing);

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode? trivia)
                => new SyntaxIdentifierWithTrivia(_contextualKind, _name, _valueText, _leading, trivia);

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics)
                => new SyntaxIdentifierWithTrivia(_contextualKind, _name, _valueText, _leading, _trailing, diagnostics, GetAnnotations());

            internal override GreenNode SetAnnotations(SyntaxAnnotation[]? annotations)
                => new SyntaxIdentifierWithTrivia(_contextualKind, _name, _valueText, _leading, _trailing, GetDiagnostics(), annotations);
        }
    }
}
