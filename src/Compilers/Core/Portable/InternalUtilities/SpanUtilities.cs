// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Loretta.CodeAnalysis
{
    internal static class SpanUtilities
    {
        public static bool All<TElement, TParam>(this ReadOnlySpan<TElement> span, TParam param, Func<TElement, TParam, bool> predicate)
        {
            foreach (var e in span)
            {
                if (!predicate(e, param))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
