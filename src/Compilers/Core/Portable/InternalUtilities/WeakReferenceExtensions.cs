﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Loretta.Utilities
{
    // Helpers that are missing from Dev11 implementation:
    internal static class WeakReferenceExtensions
    {
        public static T? GetTarget<T>(this WeakReference<T> reference) where T : class?
        {
            reference.TryGetTarget(out var target);
            return target;
        }

        public static bool IsNull<T>(this WeakReference<T> reference) where T : class?
        {
            return !reference.TryGetTarget(out _);
        }
    }
}
