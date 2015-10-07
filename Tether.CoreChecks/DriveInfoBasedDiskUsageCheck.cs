using System;
using System.Collections;
using System.IO;
using System.Threading;
using NLog;
using Tether.Plugins;

namespace Tether.CoreChecks
{

    public class DriveInfoBasedDiskUsageCheck : ICheck
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        #region ICheck Members

        public string Key => "diskUsage";

        public new object DoCheck()
        {

            ArrayList results = new ArrayList();

            Thread t = new Thread(new ThreadStart(delegate ()
            {

                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo info in drives)
                {
                    if (!info.IsReady)
                        continue;

                    try
                    {
                        string fileSystem = info.DriveFormat;
                        ulong available = (ulong)info.TotalFreeSpace;
                        ulong totalSize = (ulong)info.TotalSize;
                        string mountedOn = info.Name.TrimEnd('\\');
                        ulong used = totalSize - available;

                        results.Add(new object[] { fileSystem, "", Gigabytes(used), Gigabytes(totalSize), (int)(((float)used / (float)totalSize) * 100), mountedOn });
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }
            }));

            t.Start();
            bool completed = t.Join(10000);
            if (!completed)
            {
                t.Abort();
            }

            return results;
        }

        #endregion

        protected ulong Gigabytes(ulong value)
        {
            return value / 1024 / 1024 / 1024;
        }
    }
}