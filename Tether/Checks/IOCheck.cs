using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    /// <summary>
    /// Class for checking IO stats on disks
    /// </summary>
    public class IOCheck : IPluginCheck
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private const string PhsicalDiskCategoryName = "LogicalDisk";
        private PerformanceCounterCategory perfCategory;
        Thread counterThread;
        private List<Drive> drivesToCheck;
        private ManagementObjectSearcher searcher;

        public IOCheck()
        {    
            drivesToCheck = new List<Drive>();
            perfCategory = new PerformanceCounterCategory(PhsicalDiskCategoryName);

            searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT * FROM Win32_PerfFormattedData_PerfDisk_LogicalDisk");

            var instanceNames = searcher.Get().Cast<ManagementObject>().Select(e => e["Name"].ToString()).ToArray();

            foreach (var instance in instanceNames)
            {
                var drive = new Drive();

                drive.DriveName = instance;
                //drive.DriveName = GetDriveNameForMountPoint(instance);

                drive.InstanceName = instance;
                drive.Metrics = new List<DriveMetric>
                        {
                            new DriveMetric
                            {
                                MetricName = "rkB/s",
                                Counter = new PerformanceCounter(PhsicalDiskCategoryName, "Disk Read Bytes/sec", instance),
                                Divisor = 1024
                            },
                            new DriveMetric
                            {
                                MetricName = "wkB/s",
                                Counter = new PerformanceCounter(PhsicalDiskCategoryName, "Disk Write Bytes/sec", instance),
                                Divisor = 1024
                            },
                            new DriveMetric
                            {
                                MetricName = "%util",
                                Counter = new PerformanceCounter(PhsicalDiskCategoryName, "% Disk Time", instance),
                                Divisor = 1
                            },
                            new DriveMetric
                            {
                                MetricName = "avgqu-sz",
                                Counter = new PerformanceCounter(PhsicalDiskCategoryName, "Avg. Disk Queue Length", instance),
                                Divisor = 1
                            },
                            new DriveMetric
                            {
                                MetricName = "r/s",
                                Counter = new PerformanceCounter(PhsicalDiskCategoryName, "Disk Reads/sec", instance),
                                Divisor = 1
                            },
                            new DriveMetric
                            {
                                MetricName = "w/s",
                                Counter = new PerformanceCounter(PhsicalDiskCategoryName, "Disk Writes/sec", instance),
                                Divisor = 1
                            },
                            new DriveMetric
                            {
                                MetricName = "svctm",
                                Counter = new PerformanceCounter(PhsicalDiskCategoryName, "Avg. Disk sec/Transfer", instance),
                                Divisor = 1
                            }
                        };


                this.drivesToCheck.Add(drive);
            }


            counterThread = new Thread(GetNextCounterValueToIgnore);
            counterThread.Start();
        }

        public List<Metric> GetMetrics()
        {
            List<Metric> retr = new List<Metric>();

            foreach (var drive in this.drivesToCheck)
            {
                var tags = new Dictionary<string, string>();

                tags.Add("drive", drive.DriveName);

                retr.Add(new Metric("system.io.util", GetMetricValue(drive.Metrics, "%util"), tags: tags ));
                retr.Add(new Metric("system.io.avgqu-sz", GetMetricValue(drive.Metrics, "avgqu-sz"), tags: tags));
                retr.Add(new Metric("system.io.svctm", GetMetricValue(drive.Metrics, "svctm"), tags: tags));
                retr.Add(new Metric("system.io.rkB_s", GetMetricValue(drive.Metrics, "rkB/s"), tags: tags));
                retr.Add(new Metric("system.io.r_s", GetMetricValue(drive.Metrics, "r/s"), tags: tags));
                retr.Add(new Metric("system.io.wkB_s", GetMetricValue(drive.Metrics, "wkB/s"), tags: tags));
                retr.Add(new Metric("system.io.w_s", GetMetricValue(drive.Metrics, "w/s"), tags: tags));
            }


            return retr;
        }

        private float GetMetricValue(List<DriveMetric> metrics, string Name)
        {
            var firstOrDefault = metrics.FirstOrDefault(r=>r.MetricName == Name);
            if (firstOrDefault == null)
            {
                return 0;
            }

            var val = firstOrDefault.Counter.NextValue();

            if (firstOrDefault.Divisor > 1)
            {
                val = val / firstOrDefault.Divisor;
            }

            return val;
        }

        private void GetNextCounterValueToIgnore()
        {
            logger.Trace("GetNextCounterValueToIgnore Start");
            foreach (Drive drive in drivesToCheck)
            {
                foreach (DriveMetric metric in drive.Metrics)
                {
                    metric.Counter.NextValue();
                }
            }
            logger.Trace("GetNextCounterValueToIgnore Stop");
        }

        private string GetDriveNameForMountPoint(string DriveID)
        {
            try
            {

                if (DriveID.Contains(" "))
                {
                    return DriveID.Split(new char[1] { ' ' }, 2)[1];
                }

                var searcher = new ManagementObjectSearcher(@"Root\Microsoft\Windows\Storage", $@"SELECT * FROM MSFT_Partition WHERE DiskNumber='{DriveID}'");

                foreach (string[] wibble in from ManagementObject ob in searcher.Get() where ob["AccessPaths"] != null select ob["AccessPaths"] as string[])
                {
                    return wibble.FirstOrDefault(f => !f.Contains(@"\\?\"));
                }
            }
            catch (Exception e)
            {
                logger.Debug(e, "Error with MSFT");
            }
            return DriveID.ToString();
        }

        private class DriveMetric
        {
            public PerformanceCounter Counter { get; set; }
            public string MetricName { get; set; }
            public int Divisor { get; set; }
        }
        private class Drive
        {
            public string InstanceName { get; set; }
            public string DriveName { get; set; }
            public List<DriveMetric> Metrics { get; set; }
        }

    }
}