using System.Collections.Generic;

namespace Tether.Plugins
{
    public interface IMetricProvider
    {
        List<Metric> GetMetrics();
    }
}