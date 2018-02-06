//using System.Collections;
//using System.Management;
//using System.Text;
//using System.Threading.Tasks;
//using Tether.Plugins;
//using Timer = System.Threading.Timer;

//namespace Tether.CoreChecks
//{
//    /// <summary>
//    /// Class for checking disk usage.
//    /// </summary>
//    public class DiskUsageCheck : ICheck
//    {
//        #region ICheck Members

//        public string Key => "diskUsage";

//        public object DoCheck()
//        {
//            var results = new ArrayList();
//            // A DriveType of 3 indicates a local disk.
//            using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3"))
//            {
//                var list = query.Get();
//                using (list)
//                {
//                    foreach (var drive in list)
//                    {
//                        object fileSystemValue = drive.GetPropertyValue("FileSystem");
//                        object availableValue = drive.GetPropertyValue("FreeSpace");
//                        object totalSizeValue = drive.GetPropertyValue("Size");
//                        object mountedOnValue = drive.GetPropertyValue("DeviceID");

//                        string fileSystem = fileSystemValue == null ? string.Empty : (string)fileSystemValue;
//                        ulong available = (ulong?) availableValue ?? 0;
//                        ulong totalSize = (ulong?) totalSizeValue ?? 0;
//                        ulong used = totalSize - available;
//                        var mountedOn = mountedOnValue == null ? string.Empty : (string)mountedOnValue;
//                        int percentUsed = 0;

//                        if (totalSize > 0)
//                        {
//                            percentUsed = (int)(((float)used / (float)totalSize) * 100);
//                        }

//                        results.Add(new object[] { fileSystem, "", Gigabytes(used), Gigabytes(totalSize), percentUsed, mountedOn });
//                    }
//                    return results;
//                }
//            }
//        }

//        #endregion

//        protected ulong Gigabytes(ulong value)
//        {
//            return value / 1024 / 1024 / 1024;
//        }
//    }
//}
