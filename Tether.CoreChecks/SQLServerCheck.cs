using System;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    /// <summary>
    /// Class for checking various SQL Server metrics.
    /// </summary>
    public class SQLServerCheck : ICheck
    {
        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="SQLServerCheck"/> class.
        /// </summary>
        public SQLServerCheck(string customPrefix)
        {
            _customPrefix = customPrefix;
            _counters = new List<PerformanceCounter>();

            if (String.IsNullOrEmpty(_customPrefix))
            {
                _customPrefix = "SQLServer";
            }

            // Locks.
            AddPerformanceCounter(string.Format("{0}:Locks", _customPrefix), "Average Wait Time (ms)", "_Total");
            AddPerformanceCounter(string.Format("{0}:Locks", _customPrefix), "Lock Requests/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Locks", _customPrefix), "Lock Timeouts (timeout > 0)/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Locks", _customPrefix), "Lock Timeouts/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Locks", _customPrefix), "Lock Wait Time (ms)", "_Total");
            AddPerformanceCounter(string.Format("{0}:Locks", _customPrefix), "Lock Waits/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Locks", _customPrefix), "Number of Deadlocks/sec", "_Total");

            // Databases.
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Data File(s) Size (KB)", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log File(s) Size (KB)", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log File(s) Used Size (KB)", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Percent Log Used", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Active Transactions", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Transactions/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Repl. Pending Xacts", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Repl. Trans. Rate", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Cache Reads/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Cache Hit Ratio", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Bulk Copy Rows/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Bulk Copy Throughput/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Backup/Restore Throughput/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "DBCC Logical Scan Bytes/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Shrink Data Movement Bytes/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Flushes/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Bytes Flushed/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Flush Waits/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Flush Wait Time", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Truncations", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Growths", "_Total");
            AddPerformanceCounter(string.Format("{0}:Databases", _customPrefix), "Log Shrinks", "_Total");

            // Errors.
            AddPerformanceCounter(string.Format("{0}:SQL Errors", _customPrefix), "Errors/sec", "_Total");

            // Plan cache.
            AddPerformanceCounter(string.Format("{0}:Plan Cache", _customPrefix), "Cache Hit Ratio", "_Total");
            AddPerformanceCounter(string.Format("{0}:Plan Cache", _customPrefix), "Cache Pages", "_Total");
            AddPerformanceCounter(string.Format("{0}:Plan Cache", _customPrefix), "Cache Object Counts", "_Total");
            AddPerformanceCounter(string.Format("{0}:Plan Cache", _customPrefix), "Cache Objects in use", "_Total");

            // Cursor manager.
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Cache Hit Ratio", "_Total");
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Cached Cursor Counts", "_Total");
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Cursor Cache Use Counts/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Cursor Requests/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Active cursors", "_Total");
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Cursor memory usage", "_Total");
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Cursor worktable usage", "_Total");
            AddPerformanceCounter(string.Format("{0}:Cursor Manager by Type", _customPrefix), "Number of active cursor plans", "_Total");

            // Broker.
            AddPerformanceCounter(string.Format("{0}:Broker Activation", _customPrefix), "Tasks Started/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Broker Activation", _customPrefix), "Tasks Running", "_Total");
            AddPerformanceCounter(string.Format("{0}:Broker Activation", _customPrefix), "Tasks Aborted/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Broker Activation", _customPrefix), "Task Limit Reached/sec", "_Total");
            AddPerformanceCounter(string.Format("{0}:Broker Activation", _customPrefix), "Task Limit Reached", "_Total");
            AddPerformanceCounter(string.Format("{0}:Broker Activation", _customPrefix), "Stored Procedures Invoked/sec", "_Total");

            // Catalogger.
            AddPerformanceCounter(string.Format("{0}:Catalog Metadata", _customPrefix), "Cache Hit Ratio", "_Total");
            AddPerformanceCounter(string.Format("{0}:Catalog Metadata", _customPrefix), "Cache Entries Count", "_Total");
            AddPerformanceCounter(string.Format("{0}:Catalog Metadata", _customPrefix), "Cache Entries Pinned Count", "_Total");
        }
        #endregion

        #region ICheck Members
        public string Key
        {
            get { return "sqlServer"; }
        }

        public object DoCheck()
        {
            Dictionary<string, Dictionary<string, object>> values = new Dictionary<string, Dictionary<string, object>>();

            foreach (PerformanceCounter counter in _counters)
            {
                string objectName = counter.CategoryName.Trim().Split(':')[1];

                if (!values.ContainsKey(objectName))
                {
                    values.Add(objectName, new Dictionary<string, object>());
                }

                values[objectName].Add(counter.CounterName, counter.NextValue());
            }

            return values;
        }
        #endregion

        private void AddPerformanceCounter(string category, string counter, string instance)
        {

            try
            {
                _counters.Add(new PerformanceCounter(category, counter, instance));
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error on {category}, {counter}, {instance}");
            }

        }

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private string _customPrefix;
        private IList<PerformanceCounter> _counters;
    }
}