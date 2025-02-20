// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

namespace SimHub.Plugins.ComputedProperties.Performance
{
    /// <summary>
    /// Collects performance data.
    /// </summary>
    public class PerfData
    {
        public int Calls { get; set; }
        public int Skipped { get; set; }
        public double Time { get; set; }
        public double Duration => Time / Calls;
    }
}
