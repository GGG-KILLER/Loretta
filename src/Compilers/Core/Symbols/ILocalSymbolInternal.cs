﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Loretta.CodeAnalysis.Symbols
{
    internal interface ILocalSymbolInternal : ISymbolInternal
    {
        bool IsImportedFromMetadata { get; }

        SynthesizedLocalKind SynthesizedKind { get; }

        SyntaxNode GetDeclaratorSyntax();
    }
}