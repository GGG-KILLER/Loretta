﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Loretta.CodeAnalysis.Diagnostics;

namespace Loretta.CodeAnalysis
{
    /// <summary>
    /// Base class for logging compiler diagnostics.
    /// </summary>
    internal abstract class ErrorLogger
    {
        public abstract void LogDiagnostic(Diagnostic diagnostic, SuppressionInfo? suppressionInfo);
    }
}
