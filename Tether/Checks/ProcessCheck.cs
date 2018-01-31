using System;
using System.Collections;
using System.Collections.Generic;
using System.Management;
using NLog;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    /// <summary>
    /// Class for checking process running.
    /// </summary>
    public class ProcessCheck : ICheck
    {
        #region ICheck Members

        public string Key => "processes";

        public ProcessCheck()
        {
            _totalMemory = TotalMemory();
        }

        public virtual object DoCheck()
        {
            var processStats = ProcessStats();
            var results = new ArrayList();
            using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_Process"))
            {
                foreach (ManagementObject process in query.Get())
                {
                    try
                    {
                        var processId = (uint)process.GetPropertyValue("ProcessId");
                        var imageName = (string)process.GetPropertyValue("Name");

                        // Ignore System Idle Process for now
                        if (imageName.ToLower() == "system idle process")
                        {
                            continue;
                        }

                        var fullUserName = string.Empty;
                        var outParameters = process.InvokeMethod("GetOwner", null, null);

                        if (outParameters["User"] != null)
                        {
                            fullUserName = $@"{outParameters["Domain"]}\{outParameters["User"]}";
                        }

                        ulong[] stats = null;
                        try
                        {
                            stats = processStats[processId];
                        }
                        catch
                        {
                            logger.Info("ProcessID {0} appears to have gone missing, proceeding without it", processId.ToString());
                            continue;
                        }

                        var cpuPercentage = stats[0];
                        var workingSet = stats[1];

                        var totalMemory = _totalMemory;

                        decimal memoryPercentage = 0;

                        if (totalMemory > 0)
                        {
                            memoryPercentage = Decimal.Round((decimal)workingSet / totalMemory * 100, 2);
                        }

                        results.Add(new object[] { processId, imageName, fullUserName, cpuPercentage, memoryPercentage, workingSet });

                        //TODO : Replace with something!
                        // flag check
                        //if (Agent.Flags.ContainsKey("ProcessCheck"))
                        //{
                        //    if (imageName == Agent.Flags["ProcessCheck"])
                        //    {
                        //        bool perf = PerformanceCounterBasedProcessCheck.IsProcessRunning(imageName);
                        //        if (!perf)
                        //        {
                        //            logger.Error("Process Check: '" + Agent.Flags["ProcessCheck"] + "' process does not show in Perf Counters.");
                        //        }
                        //    }
                        //}

                    }
                    catch (ManagementException ex)
                    {
                        // Process could have ended before reaching this point in the loop
                        if (ex.Message.ToLower() == "not found")
                        {
                            continue;
                        }
                    }
                }

                return results;
            }
        }

        #endregion

        private Dictionary<uint, ulong[]> ProcessStats()
        {
            var processStats = new Dictionary<uint, ulong[]>();
            // IDProcess is not necessarily the ProcessId of a process, but seems to work
            using (var query = new ManagementObjectSearcher("SELECT IDProcess, PercentProcessorTime, WorkingSet FROM Win32_PerfFormattedData_PerfProc_Process"))
            {
                foreach (ManagementObject obj in query.Get())
                {
                    using (obj)
                    {
                        var key = (uint)obj.GetPropertyValue("IDProcess");
                        processStats[key] = new ulong[] { (ulong)obj.GetPropertyValue("PercentProcessorTime"), (ulong)obj.GetPropertyValue("WorkingSet") };
                    }
                }
                return processStats;
            }
        }

        protected virtual ulong TotalMemory()
        {
            try
            {
                using (var query = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in query.Get())
                    {
                        using (obj)
                        {
                            return (ulong)obj.GetPropertyValue("TotalVisibleMemorySize") * 1024;
                        }
                    }
                    return 0;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return 0;
        }

        protected readonly ulong _totalMemory;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static bool IsProcessRunning(string processName)
        {
            bool found = false;
            using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_Process"))
            {
                foreach (ManagementObject process in query.Get())
                {
                    if (processName == (string)process.GetPropertyValue("Name"))
                        found = true;
                }
            }
            return found;
        }
    }
}