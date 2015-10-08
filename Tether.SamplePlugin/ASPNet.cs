using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tether.Plugins;

namespace Tether.SamplePlugin
{
    public class ASPNet : ICheck
    {
        public string Key
        {
            get { return "ASPNET"; }
        }

        public object DoCheck()
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory("ASP.NET");
            IDictionary<string, object> values = new Dictionary<string, object>();

            foreach (PerformanceCounter counter in category.GetCounters())
            {
                values.Add(counter.CounterName, counter.NextValue());
            }
            return values;
        }
    }
}
