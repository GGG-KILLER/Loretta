﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis.Syntax
{
    internal partial class SyntaxList
    {
        internal class WithTwoChildren : SyntaxList
        {
            private SyntaxNode? _child0;
            private SyntaxNode? _child1;

            internal WithTwoChildren(InternalSyntax.SyntaxList green, SyntaxNode? parent, int position)
                : base(green, parent, position)
            {
            }

            internal override SyntaxNode? GetNodeSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return this.GetRedElement(ref _child0, 0);
                    case 1:
                        return this.GetRedElementIfNotToken(ref _child1);
                    default:
                        return null;
                }
            }

            internal override SyntaxNode? GetCachedSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    default:
                        return null;
                }
            }

        }
    }
}
