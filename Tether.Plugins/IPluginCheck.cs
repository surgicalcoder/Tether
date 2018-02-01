using System.Collections.Generic;

namespace Tether.Plugins
{
    public interface IPluginCheck
    {
        List<Metric> GetMetrics();
    }
}