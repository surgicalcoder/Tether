using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tether.Plugins;

namespace Tether.CoreSlices
{
      [PerformanceCounterGrouping("Win32_PerfFormattedData_PerfOS_Processor", SelectorEnum.Each)]
    public class ProcessorSlice
    {
        public long DPCRate { get; set; }

        public int InterruptsPersec { get; set; }

        public long PercentC1Time { get; set; }

        public long PercentC2Time { get; set; }

        public long PercentC3Time { get; set; }

        public long PercentIdleTime { get; set; }

        public long PercentInterruptTime { get; set; }

        public long PercentPrivilegedTime { get; set; }

        public long PercentProcessorTime { get; set; }

        public long PercentUserTime { get; set; }
    }
}
