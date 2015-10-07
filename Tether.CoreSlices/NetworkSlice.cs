using System;
using Tether.Plugins;

namespace Tether.CoreSlices
{
    [PerformanceCounterGrouping("Win32_PerfFormattedData_Tcpip_NetworkInterface", SelectorEnum.Except, ExclusionContains = new[] { "Teredo", "isatap." }, Subquery = "select Description from Win32_NetworkAdapterConfiguration where IPEnabled=True")]
    public class NetworkSlice
    {
        public string Name { get; set; }
        public UInt32 BytesSentPersec { get; set; }

        public UInt32 BytesReceivedPersec { get; set; }

        public UInt32 BytesTotalPersec { get; set; }
    }
}