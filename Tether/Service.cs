using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;
using Tether.Config;
using Tether.CoreChecks;
using Tether.Metrics;
using Tether.Plugins;
using Topshelf;
using Utilities.DataTypes.ExtensionMethods;
using Utilities.IO.ExtensionMethods;
using Timer = System.Timers.Timer;

namespace Tether
{
    public class Service : ServiceControl
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private Timer timer;
		private bool systemStatsSent = false;
	    private bool pluginChangeDetected = false;
		private List<string> ICheckTypeList;
	    private List<string> sliceCheckList;
	    
		private List<ICheck> sdCoreChecks;
	    private List<IMetricProvider> sdCoreMetrics;

        
	    private InstanceProxy instanceProxy = null;
	    private Thread pluginDetectionThread;

        public Service()
		{
		    this.sdCoreMetrics = new List<IMetricProvider>();
		    logger.Trace("start ctor");
			timer = new Timer(ConfigurationSingleton.Instance.Config.CheckInterval*1000);
			timer.Elapsed += Timer_Elapsed;

            sdCoreChecks = new List<ICheck>();
			pluginDetectionThread = new Thread(DetectAndCreate);
			pluginDetectionThread.Start();

			logger.Trace("end ctor");
		}

		private void DetectAndCreate()
		{
			DetectPlugins();

			CreateBaseChecks();
		}
	    
        private void CreateBaseChecks()
		{
			logger.Debug("Creating Base Checks...");

			//sdCoreChecks.Add(CreateCheck<DriveInfoBasedDiskUsageCheck>());
			sdCoreChecks.Add(CreateCheck<ProcessorCheck>());
			sdCoreChecks.Add(CreateCheck<ProcessCheck>());
			sdCoreChecks.Add(CreateCheck<IOCheck>());
			sdCoreChecks.Add(CreateCheck<PhysicalMemoryFreeCheck>());
			sdCoreChecks.Add(CreateCheck<PhysicalMemoryUsedCheck>());
			sdCoreChecks.Add(CreateCheck<PhysicalMemoryCachedCheck>());
			sdCoreChecks.Add(CreateCheck<SwapMemoryFreeCheck>());
			sdCoreChecks.Add(CreateCheck<SwapMemoryUsedCheck>());
			sdCoreChecks.Add(CreateCheck<IISCheck>());

		    sdCoreMetrics.Add(CreatePluginCheck<DiskUsageMetricProvider>());
		    sdCoreMetrics.Add(CreatePluginCheck<NetworkTrafficMetricProvider>());
		    sdCoreMetrics.Add(CreatePluginCheck<TetherMetricProvider>());
		    sdCoreMetrics.Add(CreatePluginCheck<CPUUtilisationMetricProvider>());

            logger.Debug("Base Check Creation Complete...");
		}

	    private IMetricProvider CreatePluginCheck<T>() where T : IMetricProvider, new()
	    {
	        logger.Trace("Creating " + typeof(T).Name);

	        T item;
	        try
	        {
	            item = new T();
	        }
	        catch (Exception e)
	        {
	            logger.Trace(e, "Error when creating " + typeof(T).Name);
	            throw;
	        }

	        logger.Trace("Finished Creating " + typeof(T).Name);

	        return item;
        }

		private ICheck CreateCheck<T>() where T: ICheck, new()
		{
			logger.Trace("Creating " + typeof(T).Name);

			T item;
			try
			{
				item = new T();
			}
			catch (Exception e)
			{
				logger.Trace(e, "Error when creating " + typeof(T).Name);
				throw;
			}

			logger.Trace("Finished Creating " + typeof(T).Name);

			return item;
		}

		private string basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		private void DetectPlugins()
		{
		    ICheckTypeList = new List<string>();
		    sliceCheckList = new List<string>();

            var pluginPath = Path.Combine(basePath, "plugins");
		    var workingPluginFolder = Path.Combine(pluginPath, "_working");

			if (!Directory.Exists(pluginPath))
			{
				return;
			}

            logger.Trace("Finding plugins");

		    if (File.Exists(Path.Combine(pluginPath, "Tether.Plugins.dll")))
		    {
                File.Delete(Path.Combine(pluginPath, "Tether.Plugins.dll"));
		    }

		    if (File.Exists(Path.Combine(pluginPath, "Tether.Plugins.pdb")))
		    {
                File.Delete(Path.Combine(pluginPath, "Tether.Plugins.pdb"));
		    }

            File.Copy(Path.Combine(basePath, "Tether.Plugins.dll"),Path.Combine(pluginPath, "Tether.Plugins.dll"));
            File.Copy(Path.Combine(basePath, "Tether.Plugins.pdb"),Path.Combine(pluginPath, "Tether.Plugins.pdb"));

            var di = new DirectoryInfo(pluginPath);
            var workingFiles = new DirectoryInfo(workingPluginFolder);
			var fileInfo = di.GetFiles("*.dll");


		    if (!fileInfo.Any())
		    {
		        return;
		    }

		    if (ConfigurationSingleton.Instance.PluginAppDomain != null)
		    {
		        AppDomain.Unload(ConfigurationSingleton.Instance.PluginAppDomain);

		        ConfigurationSingleton.Instance.PluginAppDomain = null;
		        instanceProxy = null;
		    }

		    if (Directory.Exists(workingPluginFolder))
		    {
		        try
		        {
		            Directory.Delete(workingPluginFolder, true);
		        }
		        catch (Exception e)
		        {
                    logger.Warn(e, "Error when deleting working files");
		        }
		    }

		    Directory.CreateDirectory(workingPluginFolder);

		    var wpfi = new DirectoryInfo(workingPluginFolder);
		    wpfi.SetAttributes(FileAttributes.Hidden);

		    di.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly).ForEachParallel(
		        delegate (FileInfo info1)
		        {
		            info1.CopyTo(Path.Combine(workingPluginFolder, Path.GetFileName(info1.Name)));
		        });


		    ConfigurationSingleton.Instance.PluginAppDomain = AppDomain.CreateDomain("TetherPlugins", null, new AppDomainSetup
		    {
		        ApplicationBase = workingPluginFolder,
		        PrivateBinPath = workingPluginFolder,
		        PrivateBinPathProbe = workingPluginFolder
            });

		    instanceProxy = ConfigurationSingleton.Instance.PluginAppDomain.CreateInstanceFromAndUnwrap(typeof(Service).Assembly.Location, typeof(InstanceProxy).FullName) as InstanceProxy;

		    if (instanceProxy == null)
		    {
		        logger.Warn("Instance Proxy is null, no plugins will be loaded.");
		        return;
		    }

            foreach (var info in workingFiles.GetFiles("*.dll"))
			{
				try
				{
                    var def = AssemblyDefinition.ReadAssembly(info.FullName);

				    var it = def.MainModule.Types.Where(e => e.Interfaces.Any(r => (r.FullName == typeof(IMetricProvider).FullName)) ||  (e.Interfaces.Any(r => r.FullName == typeof(ILongRunningMetricProvider).FullName))).ToList();
                    
                    var isPlugin = false;

				    if (it.Any())
				    {
				        var res = instanceProxy.LoadLibrary(info.FullName);
				        ICheckTypeList.Add(res);
				        isPlugin = true;
                    }
                    
                    if (isPlugin)
                    {
                        logger.Debug("Loaded plugin " + info);

                        ConfigurationSingleton.Instance.PluginAssemblies.Add(def.Name);
                    }
                }
				catch (Exception e)
				{
					logger.Warn(e, $"Unable to load {info.FullName}");
				}
			}

            var checkNames = ICheckTypeList.ToList();

            foreach (var jsonFiles in workingFiles.GetFiles("*.json"))
            {
                var filename = Path.GetFileNameWithoutExtension(jsonFiles.Name);
                if (checkNames.Contains(filename))
                {
                    
                    //instanceProxy.PluginSettings.Add(filename, value);
                    instanceProxy.AddSettings(filename, File.ReadAllText(jsonFiles.FullName));
                }
            }

            ICheckTypeList = ICheckTypeList.Distinct().ToList();

		    pluginChangeDetected = false;

            watcher = new FileSystemWatcher(pluginPath, "*.dll");
            watcher.Created += delegate { pluginChangeDetected = true; };
            watcher.Deleted += delegate { pluginChangeDetected = true; };
            watcher.Created += delegate { pluginChangeDetected = true; };
            watcher.Renamed += delegate { pluginChangeDetected = true; };
		    watcher.EnableRaisingEvents = true;

            logger.Trace("Plugin Load Complete");
		}
	    FileSystemWatcher watcher;
        ExpandoObjectConverter eoConverter = new ExpandoObjectConverter();

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			var results = new Dictionary<string, object>();
			var objList = new List<dynamic>();

			if (systemStatsSent)
			{
				sdCoreChecks.RemoveAll(f => f.Key == "systemStats");
			}

			systemStatsSent = true;
			logger.Info("Polling Checks");
			Parallel.ForEach(
				sdCoreChecks,
				check =>
				{

					logger.Debug($"{check.GetType()}: start");

					try
					{

						var result = check.DoCheck();

						if (result == null)
						{
							return;
						}

						results.Add(check.Key, result);

						logger.Debug("{0}: end", check.GetType());
					}
					catch (Exception ex)
					{
						logger.Error(ex, $"Error on {check.GetType()}");
					}

				});

		    var pluginCollection = new List<Metric>();
            logger.Info("Polling Core Metrics");
            Parallel.ForEach(sdCoreMetrics, check =>
		    {
                logger.Trace($"{check.GetType(): start}");

		        try
		        {

		            var result = check.GetMetrics();

		            if (result == null || !result.Any())
		            {
		                return;
		            }

		            pluginCollection.AddRange(result);

		        }
		        catch (Exception ex)
		        {
		            logger.Error(ex, $"Error on {check.GetType()}");
                }

                logger.Trace($"{check.GetType(): end}");
		    });

		    logger.Info("Polling long checks");

		    try
		    {
		        var longRunningChecks = instanceProxy.GetLongRunningChecks();

		        if (longRunningChecks.Any())
		        {
		            foreach (var lrc in longRunningChecks)
		            {
		                var list = JsonConvert.DeserializeObject<List<Metric>>(lrc.Value);
		                list.ForEach(f => f.Timestamp = DateTime.UtcNow);
		                pluginCollection.Add(list);
		            }

		        }
		    }
		    catch (RemotingException remoting)
		    {
                logger.Warn("Remoting exception, will reload plugins", remoting);
		        pluginChangeDetected = true;
		    }
		    catch (Exception exception)
		    {
		        logger.Warn(exception, "Error on polling for long checks");
		    }

		    
			
			Parallel.ForEach(
				ICheckTypeList,
				check =>
				{

					logger.Debug("{0}: start", check);
					try
					{
					    var result = instanceProxy.PerformCheck(check);

						if (result == null || !result.Any())
						{
							return;
						}

					    pluginCollection.Add(result);

					    logger.Debug($"{check}: end");
					}
					catch (Exception ex)
					{
						logger.Error(ex, $"Error on {check.GetType()}");
					}

				});


		    var serializeObject = JsonConvert.SerializeObject(pluginCollection, Formatting.None, new MetricJsonConverter());

		    results.Add("metrics", JsonConvert.DeserializeObject(serializeObject));

			try
			{
				var poster = new PayloadPoster(results);
				poster.Post();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Error with sending data to SD servers");
			}

		    CheckIfNeedToReloadPlugins();
		}

	    private void CheckIfNeedToReloadPlugins()
	    {
	        if (Process.GetCurrentProcess().PrivateMemorySize64 > ConfigurationSingleton.Instance.Config.PluginMemoryLimit)
	        {
                logger.Warn($"Memory usage of Plugin AppDomain has exceeded ${ConfigurationSingleton.Instance.PluginAppDomain.MonitoringTotalAllocatedMemorySize} bytes , reloading plugins");
                DetectPlugins();
	            return;
	        }

	        if (pluginChangeDetected)
	        {
                logger.Info("Plugin file change has been detected, reloading plugins");
                DetectPlugins();
	        }
	    }

	    private static dynamic GetName(dynamic o, dynamic coll)
		{
			try
			{
				if (((Type)coll.GetType()).GetProperties().Any(f=> f.Name == "Name" ))
				{
					return ((Type)coll.GetType()).GetProperties().FirstOrDefault(f => f.Name == "Name").GetValue(coll, null);
				}
				return o.IndexOf(coll);
			}
			catch (Exception e)
			{
				logger.Error(e, "GetName");
				throw;
			}
		}

		public bool Start(HostControl hostControl)
		{
			try
			{
				timer.Enabled = true;
				return true;
			}
			catch (Exception e)
			{
				logger.Error(e);
				throw;
			}
		}

		public bool Stop(HostControl hostControl)
		{
			timer.Enabled = false;
			return true;
		}
	}
}