using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tether.Plugins;

namespace Tether.Metrics
{
    public class CPUUtilisationMetricProvider : IMetricProvider
    {
        //List<PerformanceCounter> counters;
        Dictionary<string, List<PerformanceCounter>> counters;
        PerformanceCounterCategory performanceCounterCategory;
        string[] instanceNames;

        List<string> counterNames = new List<string>
        {
            "% Interrupt Time",
            "% Privileged Time",
            "% Processor Time",
            "% User Time",
            "% Idle Time"
        };

        public CPUUtilisationMetricProvider()
        {
            
        }

        public List<Metric> GetMetrics()
        {
            if (counters == null)
            {
                counters = new Dictionary<string, List<PerformanceCounter>>();        
                
                performanceCounterCategory = new PerformanceCounterCategory("Processor");
                instanceNames = performanceCounterCategory.GetInstanceNames();

                foreach (var instanceName in instanceNames)
                {
                    var performanceCounters = performanceCounterCategory.GetCounters(instanceName);

                    var list = performanceCounters.Where(f=> counterNames.Contains(f.CounterName) ).ToList();

                    counters.Add(instanceName, list);
                }
            }

            var totalProcessorTimeCounter = counters["_Total"].FirstOrDefault(r=>r.CounterName == "% Processor Time");
            var totalProcessorTimeValue = totalProcessorTimeCounter.NextValue();

            var values =  new List<Metric>
            {
                new Metric("serverdensity.cpu.util.pct", totalProcessorTimeValue),
                new Metric("system.load.1", totalProcessorTimeValue),
            };

            foreach (var instance in counters)
            {
                values.Add(new Metric("serverdensity.cpu.gnice", 0, tags:new Dictionary<string, string>{{"device_name", instance.Key}}));
                values.Add(new Metric("serverdensity.cpu.guest", 0, tags:new Dictionary<string, string>{{"device_name", instance.Key}}));

                values.Add(new Metric("serverdensity.cpu.sys", instance.Value.FirstOrDefault(f=>f.CounterName == "% Privileged Time").NextValue(), tags:new Dictionary<string, string>{{"device_name", instance.Key}}));
                values.Add(new Metric("serverdensity.cpu.usr", instance.Value.FirstOrDefault(f=>f.CounterName == "% User Time").NextValue(), tags:new Dictionary<string, string>{{"device_name", instance.Key}}));
                values.Add(new Metric("serverdensity.cpu.idle", instance.Value.FirstOrDefault(f=>f.CounterName == "% Idle Time").NextValue(), tags:new Dictionary<string, string>{{"device_name", instance.Key}}));
                

                
            }

            /*
             *             'serverdensity.cpu.gnice': 'cpuStats',
        'serverdensity.cpu.guest': 'cpuStats',
        'serverdensity.cpu.sys': 'cpuStats',
        'serverdensity.cpu.usr': 'cpuStats',
        'serverdensity.cpu.idle': 'cpuStats',
             */
            return values;
        }
    }
}