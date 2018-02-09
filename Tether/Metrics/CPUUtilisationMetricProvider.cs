using System.Collections.Generic;
using System.Diagnostics;
using Tether.Plugins;

namespace Tether.Metrics
{
    public class CPUUtilisationMetricProvider : IMetricProvider
    {
        PerformanceCounter counter;
        // serverdensity.cpu.util.pct
        public CPUUtilisationMetricProvider()
        {
            
        }

        public List<Metric> GetMetrics()
        {
            if (counter == null)
            {
                counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
           
            return new List<Metric>
            {
                new Metric("serverdensity.cpu.util.pct", counter.NextValue()),
                new Metric("system.load.1", counter.NextValue()),

            };
        }
    }
}