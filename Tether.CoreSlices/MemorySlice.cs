using Tether.Plugins;

namespace Tether.CoreSlices
{
    [PerformanceCounterGrouping("Win32_OperatingSystem", SelectorEnum.Single)]
    public class MemorySlice
    {
        [PerformanceCounterValue(Divisor = 1024)]
        public int TotalVisibleMemorySize { get; set; }
        [PerformanceCounterValue(Divisor = 1024)]
        public int TotalVirtualMemorySize { get; set; }
        [PerformanceCounterValue(Divisor = 1024)]
        public int SizeStoredInPagingFiles { get; set; }
        [PerformanceCounterValue(Divisor = 1024)]
        public int FreePhysicalMemory { get; set; }
        [PerformanceCounterValue(Divisor = 1024)]
        public int FreeVirtualMemory { get; set; }
        [PerformanceCounterValue(Divisor = 1024)]
        public int FreeSpaceInPagingFiles { get; set; }
    }
}