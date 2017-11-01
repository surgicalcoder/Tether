using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading.Tasks;
using NLog;
using Quartz;
using Tether.Plugins;
using Topshelf;
using Topshelf.Quartz;
using Utilities.DataTypes.ExtensionMethods;

namespace Tether
{
    internal class InstanceProxy : MarshalByRefObject
    {
        private Dictionary<string, ICheck> CheckTypes;
        private List<string> slices;

        public InstanceProxy()
        {
            CheckTypes = new Dictionary<string, ICheck>();
            slices = new List<string>();
        }

        public dynamic GetSlice(string Name)
        {
            MethodInfo method = GetType().GetMethod("PopulateMultiple", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(new Type[] { Type.GetType(Name) });

            var invoke = method.Invoke(this, null) as dynamic;

            return invoke;
        }

        private static List<T> PopulateMultiple<T>() where T : new()
        {
            var t = new List<T>();
            var pcga = typeof(T).Attribute<PerformanceCounterGroupingAttribute>();

            if (pcga != null)
            {

                if (pcga.UsePerformanceCounter)
                {
                    var PerfCounter = new PerformanceCounterCategory(pcga.WMIClassName);

                    var instances = PerfCounter.GetInstanceNames().PerformCounterFiltering(pcga.Selector, pcga.SelectorValue, pcga.ExclusionContains);

                    foreach (var instance in instances)
                    {
                        var item = new T();

                        IEnumerable<string> names = typeof(T).GetProperties()
                                                .Where(f => f.Attribute<PerformanceCounterValueExcludeAttribute>() == null)
                                                .Select(
                                                    delegate (PropertyInfo info)
                                                    {
                                                        if (info.Attribute<PerformanceCounterValueAttribute>() != null && info.Attribute<PerformanceCounterValueAttribute>().PropertyName != null)
                                                        {
                                                            return info.Attribute<PerformanceCounterValueAttribute>().PropertyName;
                                                        }
                                                        return info.Name;
                                                    });

                        foreach (var name in names)
                        {
                            PropertyInfo property = typeof(T).GetProperties()
                                    .FirstOrDefault(
                                        f => (f.Attribute<PerformanceCounterValueAttribute>() != null && f.Attribute<PerformanceCounterValueAttribute>().PropertyName == name) ||
                                        f.Name == name && f.Attribute<PerformanceCounterValueExcludeAttribute>() == null);


                            if (property.Attribute<PerformanceCounterInstanceNameAttribute>() != null)
                            {
                                property.SetValue(item, instance, null);
                            }
                            else
                            {
                                var performanceCounters = PerfCounter.GetCounters(instance);
                                try
                                {

                                    var changeType = Convert.ChangeType(performanceCounters.FirstOrDefault(e => e.CounterName == name).NextValue(), property.PropertyType);

                                    if (property.Attribute<PerformanceCounterValueAttribute>() != null && property.Attribute<PerformanceCounterValueAttribute>().Divisor > 0)
                                    {
                                        if (property.PropertyType == typeof(long))
                                        {
                                            changeType = (long)changeType / property.Attribute<PerformanceCounterValueAttribute>().Divisor;
                                        }
                                        else if (property.PropertyType == typeof(int))
                                        {
                                            changeType = (int)changeType / property.Attribute<PerformanceCounterValueAttribute>().Divisor;
                                        }
                                        else if (property.PropertyType == typeof(short))
                                        {
                                            changeType = (short)changeType / property.Attribute<PerformanceCounterValueAttribute>().Divisor;
                                        }
                                    }

                                    property.SetValue(item, changeType, null);
                                }
                                catch (Exception e)
                                {
                                    logger.ErrorException("Error on property " + name, e);
                                }
                                finally
                                {
                                    DisposeAll(performanceCounters);

                                }
                            }

                        }
                        t.Add(item);

                    }
                    return t;

                }


                var searcher = new ManagementObjectSearcher(pcga.WMIRoot, "SELECT * FROM " + pcga.WMIClassName);

                foreach (ManagementObject var in searcher.Get().Cast<ManagementObject>().PerformFiltering(pcga.Selector, pcga.SelectorValue, pcga.ExclusionContains, pcga.Subquery))
                {
                    var item = new T();

                    IEnumerable<string> names = typeof(T).GetProperties()
                        .Where(f => f.Attribute<PerformanceCounterValueExcludeAttribute>() == null)
                        .Select(
                            delegate (PropertyInfo info)
                            {
                                if (info.Attribute<PerformanceCounterValueAttribute>() != null && info.Attribute<PerformanceCounterValueAttribute>().PropertyName != null)
                                {
                                    return info.Attribute<PerformanceCounterValueAttribute>().PropertyName;
                                }
                                return info.Name;
                            });

                    foreach (var name in names)
                    {
                        var property = typeof(T).GetProperties()
                                .FirstOrDefault(
                                    f => (f.Attribute<PerformanceCounterValueAttribute>() != null && f.Attribute<PerformanceCounterValueAttribute>().PropertyName == name) || f.Name == name && f.Attribute<PerformanceCounterValueExcludeAttribute>() == null);

                        try
                        {

                            var changeType = Convert.ChangeType(var[name], property.PropertyType);

                            if (property.Attribute<PerformanceCounterValueAttribute>() != null && property.Attribute<PerformanceCounterValueAttribute>().Divisor > 0)
                            {
                                if (property.PropertyType == typeof(long))
                                {
                                    changeType = (long)changeType / property.Attribute<PerformanceCounterValueAttribute>().Divisor;
                                }
                                else if (property.PropertyType == typeof(int))
                                {
                                    changeType = (int)changeType / property.Attribute<PerformanceCounterValueAttribute>().Divisor;
                                }
                                else if (property.PropertyType == typeof(short))
                                {
                                    changeType = (short)changeType / property.Attribute<PerformanceCounterValueAttribute>().Divisor;
                                }
                            }



                            property.SetValue(item, changeType, null);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, $"Error on property {name}");
                        }

                    }
                    t.Add(item);
                }


            }


            return t;
        }


        public dynamic PerformCheck(string checkName)
        {
            if (string.IsNullOrWhiteSpace(checkName))
            {
                throw new ArgumentException("message", nameof(checkName));
            }

            return CheckTypes[checkName].DoCheck() as dynamic;
        }

        public List<string> LoadSlices(string path)
        {
            Assembly asm = Assembly.LoadFrom(path);

            var types = asm.GetTypes().Where(e => e.GetCustomAttributes(typeof(PerformanceCounterGroupingAttribute), true).Any()).ToList();

            if (!types.Any())
            {
                return null;
            }
            List<string> retr = new List<string>();
            foreach (var type in types)
            {
                slices.Add(type.FullName);
                retr.Add(type.FullName);
            }
            return retr;
        }

        public List<String> LoadLibrary(string path)
        {
            Assembly asm = Assembly.LoadFrom(path);

            var enumerable = asm.GetTypes().Where(r=> r.GetInterfaces().Any(e=>e.FullName == typeof(ICheck).FullName)  ).ToList();

            if (enumerable.Any())
            {
                foreach (var type in enumerable)
                {
                    if (Activator.CreateInstance(type) is ICheck check)
                    {
                        CheckTypes.Add(check.Key, check);
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
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


        static void Main(string[] args)
        {
            try
            {
                logger.Trace("Performing Host Init");

                var tempPath = Path.Combine(basePath, "plugins", "_temp");
                if (Directory.Exists(tempPath))
                {
                    if (Directory.GetFiles(tempPath).Any())
                    {
                        foreach (var file in Directory.GetFiles(tempPath))
                        {
                            File.Move(file, Path.Combine(basePath, "plugins", Path.GetFileName(file) ) );
                        }
                    }
                    Directory.Delete(tempPath, true);
                }

                Host host = HostFactory.New(x =>
                {
                    x.UseNLog();
                    x.Service<Service>(service =>
                    {
                        service.ConstructUsing(f => new Service());
                        service.WhenStarted((a, control) => a.Start(control));
                        service.WhenStopped((a, control) => a.Stop(control));

                        service.ScheduleQuartzJob(b => b.WithJob(() =>
                                JobBuilder.Create<ManifestRegularCheck>().Build())
                            .AddTrigger(() => TriggerBuilder.Create()
                                .WithSimpleSchedule(builder => builder.WithMisfireHandlingInstructionFireNow()
                                    .WithIntervalInSeconds(Config.ConfigurationSingleton.Instance.Config.ManifestCheckInterval)
                                    .RepeatForever())
                                .Build()));

                        service.ScheduleQuartzJob(b => b.WithJob(() =>
                                JobBuilder.Create<ResenderJob>().Build())
                            .AddTrigger(() => TriggerBuilder.Create()
                                .WithSimpleSchedule(builder => builder.WithMisfireHandlingInstructionFireNow()
                                    .WithIntervalInSeconds(Config.ConfigurationSingleton.Instance.Config.RetriesResendInterval)
                                    .RepeatForever())
                                .Build()));
                    });
                    x.RunAsLocalSystem();
                    x.StartAutomatically();
                    x.SetDescription("Tether");
                    x.SetDisplayName("Tether");
                    x.SetServiceName("Tether");

                    x.EnableServiceRecovery(
                        r =>
                        {
                            r.RestartService(0);
                        });
                });

                host.Run();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Problem when trying to run host");
            }

        }
    }
}
