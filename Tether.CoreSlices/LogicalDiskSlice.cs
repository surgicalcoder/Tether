using Tether.Plugins;

namespace Tether.CoreSlices
{
    [PerformanceCounterGrouping("Win32_PerfFormattedData_PerfDisk_LogicalDisk", SelectorEnum.Each)]
    public class LogicalDiskSlice
    {
        public string Name { get; set; }
        public int PercentFreeSpace { get; set; }

        public int FreeMegabytes { get; set; }

        public long AvgDiskReadQueueLength { get; set; }

        public int AvgDiskSecPerRead { get; set; }

        public int AvgDiskSecPerWrite { get; set; }

        public long AvgDiskWriteQueueLength { get; set; }

        public long DiskReadBytesPerSec { get; set; }

        public int DiskReadsPerSec { get; set; }

        public long DiskWriteBytesPerSec { get; set; }

        public int DiskWritesPerSec { get; set; }

        public long PercentDiskReadTime { get; set; }

        public long PercentDiskWriteTime { get; set; }

        public int SplitIOPerSec { get; set; }
    }
}