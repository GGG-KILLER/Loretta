using System;
using Microsoft.CodeAnalysis;

namespace Loretta.Generators
{
    internal static class Diagnostics
    {
        private static readonly string[] notConfigurableTags = new[] { WellKnownDiagnosticTags.NotConfigurable };

        public static readonly DiagnosticDescriptor SyntaxXmlNotFound = new DiagnosticDescriptor(
            id: "LOSX0001",
            title: "Syntax.xml not found",
            messageFormat: "Syntax.xml was not found so no syntax tree nodes, visitors, rewriters or factories will be generated",
            category: "Loretta.Generators.SyntaxXml",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: notConfigurableTags);

        public static readonly DiagnosticDescriptor SyntaxXmlHasNoText = new DiagnosticDescriptor(
            id: "LOSX0002",
            title: "Syntax.xml has no text",
            messageFormat: "Syntax.xml had no text when AdditionalText.GetText() was called so no syntax tree nodes, visitors, rewriters or factories will be generated",
            category: "Loretta.Generators.SyntaxXml",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: notConfigurableTags);

        public static readonly DiagnosticDescriptor SyntaxXmlError = new DiagnosticDescriptor(
            id: "LOSX0003",
            title: "Syntax.xml has a syntax error",
            messageFormat: "{0}",
            category: "Loretta.Generators.SyntaxXml",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: notConfigurableTags);
    }
}
