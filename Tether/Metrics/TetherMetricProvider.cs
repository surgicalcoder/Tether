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

            var currentProcess = Process.GetCurrentProcess();

            var values = new List<Metric>
            {
                new Metric("tether.process.memory", currentProcess.PrivateMemorySize64),
                new Metric("tether.process.threads", currentProcess.Threads.Count)
            };
            return values;
        }
    }
}
