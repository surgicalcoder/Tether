using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                new Metric("tether.process.memory", Process.GetCurrentProcess().PrivateMemorySize64)
            };
            return values;
        }
    }
}
