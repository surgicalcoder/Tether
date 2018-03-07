using System.Collections.Generic;
using System.Management;
using NLog;
using Tether.Plugins;

namespace Tether.Metrics
{
    public class DiskUsageMetricProvider : IMetricProvider
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public List<Metric> GetMetrics()
        {
            List<Metric> values = new List<Metric>();
            using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3"))
            {
                var list = query.Get();
                using (list)
                {
                    foreach (var drive in list)
                    {
                        object fileSystemValue = drive.GetPropertyValue("FileSystem");
                        object availableValue = drive.GetPropertyValue("FreeSpace");
                        object totalSizeValue = drive.GetPropertyValue("Size");
                        object mountedOnValue = drive.GetPropertyValue("DeviceID");

                        //string fileSystem = fileSystemValue == null ? string.Empty : (string)fileSystemValue;
                        ulong available = (ulong?) availableValue ?? 0;
                        ulong totalSize = (ulong?) totalSizeValue ?? 0;
                        ulong used = totalSize - available;

                        var mountedOn = mountedOnValue == null ? string.Empty : (string)mountedOnValue;

                        float percentUsed = 0;

                        if (totalSize > 0)
                        {
                            percentUsed = used / (float)totalSize;
                        }

                        values.Add(new Metric("system.disk.total", totalSize/1024, tags:new Dictionary<string, string>{{"device_name", mountedOn}}));
                        values.Add(new Metric("system.disk.used", used/1024, tags:new Dictionary<string, string>{{"device_name", mountedOn}}));
                        values.Add(new Metric("system.disk.free", available/1024, tags:new Dictionary<string, string>{{"device_name", mountedOn}}));
                        values.Add(new Metric("system.disk.in_use", percentUsed, tags:new Dictionary<string, string>{{"device_name", mountedOn}}));
                    }
                }
            }

            return values;
        }
    }
}