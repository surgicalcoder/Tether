using System.Management;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    /// <summary>
    /// Class for physical memory usage checks.
    /// </summary>
    public class PhysicalMemoryUsedCheck : ICheck
    {
        #region ICheck Members

        public string Key => "memPhysUsed";

        public object DoCheck()
        {
            ulong used = 0;
            using (var query = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                var list = query.Get();
                using (list)
                {
                    foreach (var memory in list)
                    {
                        var total = (ulong)memory.GetPropertyValue("TotalVisibleMemorySize");
                        var free = (ulong)memory.GetPropertyValue("FreePhysicalMemory");
                        used = (total - free) / 1024;
                    }
                    return used;
                }
            }
        }

        #endregion
    }
}