using System;
using System.Collections.Generic;

namespace Tether.Plugins
{
    public interface ILongRunningMetricProvider
    {
        List<Metric> GetMetrics();

        TimeSpan CacheDuration { get; }
    }
}