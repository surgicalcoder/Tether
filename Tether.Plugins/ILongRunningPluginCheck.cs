using System;
using System.Collections.Generic;

namespace Tether.Plugins
{
    public interface ILongRunningPluginCheck
    {
        List<Metric> GetMetrics();

        TimeSpan CacheDuration { get; }
    }
}