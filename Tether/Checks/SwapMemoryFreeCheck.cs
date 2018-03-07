using System.Management;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    public class SwapMemoryFreeCheck : ICheck
    {
        #region ICheck Members

        public string Key => "memSwapFree";

        public object DoCheck()
        {
            using (var query = new ManagementObjectSearcher("SELECT AllocatedBaseSize, CurrentUsage FROM Win32_PageFileUsage"))
            {
                uint total = 0;
                uint used = 0;
                foreach (ManagementBaseObject obj in query.Get())
                {
                    using (obj)
                    {
                        total += (uint)obj.GetPropertyValue("AllocatedBaseSize");
                        used += (uint)obj.GetPropertyValue("CurrentUsage");
                    }
                }
                return total - used;
            }
        }

        #endregion
    }
}