﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Diagnostics;

namespace Loretta.Utilities
{
    internal static class GeneratedCodeUtilities
    {
        private static readonly string[] s_autoGeneratedStrings = new[] { "<autogenerated", "<auto-generated" };

        internal static bool IsGeneratedCode(
            SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken)
        {
            return IsGeneratedCodeFile(tree.FilePath) ||
                   BeginsWithAutoGeneratedComment(tree, isComment, cancellationToken);
        }

        internal static bool IsGeneratedCode(string? filePath, SyntaxNode root, Func<SyntaxTrivia, bool> isComment)
        {
            return IsGeneratedCodeFile(filePath) ||
                   BeginsWithAutoGeneratedComment(root, isComment);
        }

        private static bool IsGeneratedCodeFile([NotNullWhen(returnValue: true)] string? filePath)
        {
            if (!RoslynString.IsNullOrEmpty(filePath))
            {
                var fileName = PathUtilities.GetFileName(filePath);
                if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var extension = PathUtilities.GetExtension(fileName);
                if (!string.IsNullOrEmpty(extension))
                {
                    var fileNameWithoutExtension = PathUtilities.GetFileName(filePath, includeExtension: false);
                    if (fileNameWithoutExtension.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".generated", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool BeginsWithAutoGeneratedComment(SyntaxNode root, Func<SyntaxTrivia, bool> isComment)
        {
            if (root.HasLeadingTrivia)
            {
                var leadingTrivia = root.GetLeadingTrivia();

                foreach (var trivia in leadingTrivia)
                {
                    if (!isComment(trivia))
                    {
                        continue;
                    }

                    var text = trivia.ToString();

                    // Check to see if the text of the comment contains an auto generated comment.
                    foreach (var autoGenerated in s_autoGeneratedStrings)
                    {
                        if (text.Contains(autoGenerated))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool BeginsWithAutoGeneratedComment(
            SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken)
        {
            var root = tree.GetRoot(cancellationToken);
            if (root.HasLeadingTrivia)
            {
                var leadingTrivia = root.GetLeadingTrivia();

                foreach (var trivia in leadingTrivia)
                {
                    if (!isComment(trivia))
                    {
                        continue;
                    }

                    var text = trivia.ToString();

                    // Check to see if the text of the comment contains an auto generated comment.
                    foreach (var autoGenerated in s_autoGeneratedStrings)
                    {
                        if (text.Contains(autoGenerated))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
