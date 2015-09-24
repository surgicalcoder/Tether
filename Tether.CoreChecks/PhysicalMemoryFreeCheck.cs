using System.Management;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    /// <summary>
    /// Class for checking memory availability.
    /// </summary>
    public class PhysicalMemoryFreeCheck : ICheck
    {
        #region ICheck Members

        public string Key
        {
            get { return "memPhysFree"; }
        }

        public object DoCheck()
        {
            ulong total = 0;
            using (var query = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                var list = query.Get();
                using (list)
                {
                    foreach (var memory in list)
                    {
                        total = (ulong)memory.GetPropertyValue("FreePhysicalMemory") / 1024;
                    }
                    return total;
                }
            }
        }

        #endregion
    }
}