﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loretta.Utilities
{
    internal static class SemaphoreSlimExtensions
    {
        public static SemaphoreDisposer DisposableWait(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            semaphore.Wait(cancellationToken);
            return new SemaphoreDisposer(semaphore);
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", OftenCompletesSynchronously = true)]
        public static async ValueTask<SemaphoreDisposer> DisposableWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new SemaphoreDisposer(semaphore);
        }

        [NonCopyable]
        internal struct SemaphoreDisposer : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public SemaphoreDisposer(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
