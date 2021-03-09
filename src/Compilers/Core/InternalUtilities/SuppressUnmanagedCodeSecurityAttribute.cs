﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    // The attribute is not portable, but is needed to improve perf of interop calls on desktop.
    internal class SuppressUnmanagedCodeSecurityAttribute : Attribute
    {
    }
}
