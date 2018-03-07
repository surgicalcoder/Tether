using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tether.Plugins;
using Utilities.DataTypes.ExtensionMethods;

namespace Tether
{
    internal class InstanceProxy : MarshalByRefObject
    {

        private class LongRunningResult
        {
            public string Name { get; set; }
            public List<Metric> Result { get; set; }
            public DateTime LastRun { get; set; }
            public bool IsCurrentlyRunning { get; set; }
        }
    


        private Dictionary<string, IMetricProvider> CheckTypes;
        private Dictionary<string, ILongRunningMetricProvider> LongChecks;
        private List<LongRunningResult> longRunningResults;
        private Dictionary<string, Type> slices;
        public Dictionary<string, dynamic> PluginSettings { get; set; }

        public InstanceProxy()
        {
            CheckTypes = new Dictionary<string, IMetricProvider>();
            slices = new Dictionary<string, Type>();
            PluginSettings = new Dictionary<string, dynamic>();
            LongChecks = new Dictionary<string, ILongRunningMetricProvider>();
            longRunningResults = new List<LongRunningResult>();
        }

        public void AddSettings(string name, string values)
        {
            var value = JsonConvert.DeserializeObject<ExpandoObject>(values, new ExpandoObjectConverter()) as dynamic;
            PluginSettings.Add(name, value);
        }

        public Dictionary<string, string> GetLongRunningChecks()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            if (longRunningResults.Any())
            {
                foreach (var longRunningCheck in LongChecks)
                {
                    var result = longRunningResults.FirstOrDefault(f=>f.Name == longRunningCheck.Key);

                    if (result != null)
                    {
                        var run = result.LastRun.Add(longRunningCheck.Value.CacheDuration) < DateTime.Now;

                        if (run)
                        {
                            RunLongRunningCheck(longRunningCheck);
                        }

                        results.Add(longRunningCheck.Key, JsonConvert.SerializeObject(result.Result));
                    }
                    else
                    {
                        RunLongRunningCheck(longRunningCheck);
                    }
                }
            }
            else
            {
                foreach (var longRunningCheck in LongChecks)
                {
                    RunLongRunningCheck(longRunningCheck);
                }
            }

            return results;
        }

        private void RunLongRunningCheck(KeyValuePair<string, ILongRunningMetricProvider> longRunningCheck)
        {
            var ee = longRunningResults.FirstOrDefault(f => f.Name == longRunningCheck.Key) ?? new LongRunningResult();

            if (ee.IsCurrentlyRunning)
            {
                return;
            }

            var thread = new Thread(() =>
            {
                
                ee.Name = longRunningCheck.Key;
                ee.LastRun = DateTime.Now;
                ee.IsCurrentlyRunning = true;

                longRunningResults.RemoveAll(f => f.Name == longRunningCheck.Key);
                longRunningResults.Add(ee);

                var result = longRunningCheck.Value.GetMetrics();

                ee.IsCurrentlyRunning = false;
                ee.Result = result;

                longRunningResults.RemoveAll(f => f.Name == longRunningCheck.Key);
                longRunningResults.Add(ee);
            });
            thread.Start();
        }

        public List<Metric> PerformCheck(string checkName)
        {
            if (string.IsNullOrWhiteSpace(checkName))
            {
                throw new ArgumentException("message", nameof(checkName));
            }

            var check = CheckTypes[checkName];

            if (check is IRequireConfigurationData)
            {
                if ( PluginSettings.ContainsKey(check.GetType().FullName))
                {
                    (check as IRequireConfigurationData).LoadConfigurationData(PluginSettings[check.GetType().FullName]);
                }
            }

            return check.GetMetrics();
        }

        public List<String> LoadLibrary(string path)
        {
            var asm = Assembly.LoadFrom(path);

            var longRunningChecks = asm.GetTypes().Where(r => r.GetInterfaces().Any(e => e.FullName == typeof(ILongRunningMetricProvider).FullName)).ToList();

            if (longRunningChecks.Any())
            {
                foreach (var longRunningCheck in longRunningChecks)
                {
                    if (Activator.CreateInstance(longRunningCheck) is ILongRunningMetricProvider runningCheck)
                    {
                        LongChecks.Add(longRunningCheck.FullName, runningCheck);
                    }
                }
            }

            var enumerable = asm.GetTypes().Where(r=> r.GetInterfaces().Any(e=>e.FullName == typeof(IMetricProvider).FullName)  ).ToList();

            if (enumerable.Any())
            {
                foreach (var type in enumerable)
                {
                    if (Activator.CreateInstance(type) is IMetricProvider check)
                    {
                        CheckTypes.Add(type.FullName, check);
                    }
                }

                var items = CheckTypes.Select(f => f.Key).ToList();

                return items;
            }
            else
            {
                return null;
            }
        }
    }
}