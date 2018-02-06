using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tether.Config;
using Tether.Plugins;

namespace Tether.Metrics
{
    public class TetherMetricProvider : IMetricProvider
    {
        public List<Metric> GetMetrics()
        {
            if (!Config.ConfigurationSingleton.Instance.Config.SubmitTetherData)
            {
                return null;
            }

            var values = new List<Metric>
            {
                new Metric("tether.memory", AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize, tags: new Dictionary<string, string> {{"AppDomain", "Application"}}),
                new Metric("tether.cpu", (float) AppDomain.CurrentDomain.MonitoringTotalProcessorTime.TotalSeconds, tags: new Dictionary<string, string> {{"AppDomain", "Application"}}),
                new Metric("tether.memory", ConfigurationSingleton.Instance.PluginAppDomain.MonitoringTotalAllocatedMemorySize, tags: new Dictionary<string, string> {{"AppDomain", "Plugins"}}),
                new Metric("tether.cpu", (float) ConfigurationSingleton.Instance.PluginAppDomain.MonitoringTotalProcessorTime.TotalSeconds, tags: new Dictionary<string, string> {{"AppDomain", "Plugins"}})
            };



            return values;
        }
    }
}
