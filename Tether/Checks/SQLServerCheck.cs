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
            AddPerformanceCounter($"{_customPrefix}:Locks", "Average Wait Time (ms)", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Locks", "Lock Requests/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Locks", "Lock Timeouts (timeout > 0)/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Locks", "Lock Timeouts/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Locks", "Lock Wait Time (ms)", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Locks", "Lock Waits/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Locks", "Number of Deadlocks/sec", "_Total");

            // Databases.
            AddPerformanceCounter($"{_customPrefix}:Databases", "Data File(s) Size (KB)", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log File(s) Size (KB)", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log File(s) Used Size (KB)", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Percent Log Used", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Active Transactions", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Transactions/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Repl. Pending Xacts", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Repl. Trans. Rate", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Cache Reads/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Cache Hit Ratio", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Bulk Copy Rows/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Bulk Copy Throughput/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Backup/Restore Throughput/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "DBCC Logical Scan Bytes/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Shrink Data Movement Bytes/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Flushes/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Bytes Flushed/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Flush Waits/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Flush Wait Time", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Truncations", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Growths", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Databases", "Log Shrinks", "_Total");

            // Errors.
            AddPerformanceCounter($"{_customPrefix}:SQL Errors", "Errors/sec", "_Total");

            // Plan cache.
            AddPerformanceCounter($"{_customPrefix}:Plan Cache", "Cache Hit Ratio", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Plan Cache", "Cache Pages", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Plan Cache", "Cache Object Counts", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Plan Cache", "Cache Objects in use", "_Total");

            // Cursor manager.
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Cache Hit Ratio", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Cached Cursor Counts", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Cursor Cache Use Counts/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Cursor Requests/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Active cursors", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Cursor memory usage", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Cursor worktable usage", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Cursor Manager by Type", "Number of active cursor plans", "_Total");

            // Broker.
            AddPerformanceCounter($"{_customPrefix}:Broker Activation", "Tasks Started/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Broker Activation", "Tasks Running", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Broker Activation", "Tasks Aborted/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Broker Activation", "Task Limit Reached/sec", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Broker Activation", "Task Limit Reached", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Broker Activation", "Stored Procedures Invoked/sec", "_Total");

            // Catalogger.
            AddPerformanceCounter($"{_customPrefix}:Catalog Metadata", "Cache Hit Ratio", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Catalog Metadata", "Cache Entries Count", "_Total");
            AddPerformanceCounter($"{_customPrefix}:Catalog Metadata", "Cache Entries Pinned Count", "_Total");
        }
        #endregion

        #region ICheck Members
        public string Key => "sqlServer";

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