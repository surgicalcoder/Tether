using System.Management;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    public class PhysicalMemoryCachedCheck : ICheck
    {
        #region ICheck Members

        public string Key => "memCached";

        public object DoCheck()
        {
            ulong cached = 0;
            // We'll try this for now.
            using (var query = new ManagementObjectSearcher("SELECT CacheBytes FROM Win32_PerfFormattedData_PerfOS_Memory"))
            {
                foreach (var obj in query.Get())
                {
                    using (obj)
                    {
                        cached = (ulong)obj.GetPropertyValue("CacheBytes") / 1024 / 1024;
                    }
                }
            }
            return cached;
        }

        #endregion
    }
}