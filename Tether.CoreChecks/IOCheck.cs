using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    /// <summary>
    /// Class for checking IO stats on disks
    /// </summary>
    public class IOCheck : ICheck
    {
        /// <summary>
        /// List of the physical drives to check
        /// </summary>
        private List<Drive> drivesToCheck;

        /// <summary>
        /// Initializes a new instance of the IOCheck class and set up the performance monitors we need
        /// </summary>
        public IOCheck()
        {
            this.drivesToCheck = new List<Drive>();

            var perfCategory = new PerformanceCounterCategory("PhysicalDisk");
            string[] instanceNames = perfCategory.GetInstanceNames();

            foreach (var instance in instanceNames)
            {
                // ignore _Total and other system categories
                if (instance.StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                var drive = new Drive();
                drive.DriveName = instance.Split(new char[1] { ' ' }, 2)[1];
                drive.InstanceName = instance;
                drive.Metrics = new List<DriveMetric>();

                drive.Metrics.Add(new DriveMetric() { MetricName = "rkB/s", Counter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", instance), Divisor = 1024 });
                drive.Metrics.Add(new DriveMetric() { MetricName = "wkB/s", Counter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", instance), Divisor = 1024 });
                drive.Metrics.Add(new DriveMetric() { MetricName = "%util", Counter = new PerformanceCounter("PhysicalDisk", "% Disk Time", instance), Divisor = 1 });
                drive.Metrics.Add(new DriveMetric() { MetricName = "avgqu-sz", Counter = new PerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", instance), Divisor = 1 });
                drive.Metrics.Add(new DriveMetric() { MetricName = "r/s", Counter = new PerformanceCounter("PhysicalDisk", "Disk Reads/sec", instance), Divisor = 1 });
                drive.Metrics.Add(new DriveMetric() { MetricName = "w/s", Counter = new PerformanceCounter("PhysicalDisk", "Disk Writes/sec", instance), Divisor = 1 });
                drive.Metrics.Add(new DriveMetric() { MetricName = "svctm", Counter = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Transfer", instance), Divisor = 1 });

                // take the first readings
                foreach (var c in drive.Metrics)
                {
                    c.Counter.NextValue();
                }

                this.drivesToCheck.Add(drive);
            }
        }

        /// <summary>
        /// Gets the name of the check
        /// </summary>
        public string Key
        {
            get { return "ioStats"; }
        }

        /// <summary>
        /// Run the check
        /// </summary>
        /// <returns>An object (usually a Dictionary) containing the check results</returns>
        public object DoCheck()
        {
            var results = new Dictionary<string, object>();

            foreach (var drive in this.drivesToCheck)
            {
                var driveResults = new Dictionary<string, object>();

                foreach (var metric in drive.Metrics)
                {
                    driveResults[metric.MetricName] = metric.Counter.NextValue() / metric.Divisor;
                }


                var read = (float)driveResults["r/s"];
                var write = (float)driveResults["w/s"];

                var total = read + write;
                float ratio = (read / total) * 100;

                if (!float.IsNaN(ratio))
                {
                    driveResults["rwratio"] = ratio;
                }
                else
                {
                    driveResults["rwratio"] = 0.0;
                }


                results[drive.DriveName] = driveResults;
            }

            return results;
        }

        /// <summary>
        /// A single metric to measure
        /// </summary>
        private class DriveMetric
        {
            /// <summary>
            /// Gets or sets the Performance Counter to retrieve the metric from
            /// </summary>
            public PerformanceCounter Counter { get; set; }

            /// <summary>
            /// Gets or sets the name of the metric to send to SD
            /// </summary>
            public string MetricName { get; set; }

            /// <summary>
            /// Gets or sets the number to divide result by (to convert bytes to kilobytes, etc)
            /// </summary>
            public int Divisor { get; set; }
        }

        /// <summary>
        /// Represents a physical drive to get metrics on
        /// </summary>
        private class Drive
        {
            /// <summary>
            /// Gets or sets the name that performance monitor uses for the drive
            /// </summary>
            public string InstanceName { get; set; }

            /// <summary>
            /// Gets or sets the friendly name to display in SD
            /// </summary>
            public string DriveName { get; set; }

            /// <summary>
            /// Gets or sets the list of metrics to fetch each run
            /// </summary>
            public List<DriveMetric> Metrics { get; set; }
        }
    }
}