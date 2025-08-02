// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Diagnostics;

namespace SimHub.Plugins.ComputedProperties.Performance
{
    /// <summary>
    /// Simple helper to measure performance data. Can be used in a <c>using</c> directive, which starts the stopwatch.
    /// The stopwatch will be stopped, when the <c>using</c> context is exited.
    /// </summary>
    public class PerfToken : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly PerfData _perfData;

        public PerfToken(PerfData perfData)
        {
            _stopwatch = Stopwatch.StartNew();
            _perfData = perfData;
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var ms = _stopwatch.Elapsed.TotalMilliseconds;
            _perfData.Calls++;
            _perfData.Time += _stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}