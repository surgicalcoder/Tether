using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Tether.Config;
using Tether.CoreChecks;
using Tether.Plugins;
using Topshelf;
using Utilities.DataTypes.ExtensionMethods;
using Timer = System.Timers.Timer;

namespace Tether
{
	public class Service : ServiceControl
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private Timer timer;
		private bool systemStatsSent = false;
		private List<string> ICheckTypeList;
	    private List<string> sliceCheckList;
	    //private Dictionary<string, dynamic> PluginSettings;
		Thread pluginDetectionThread;
		List<ICheck> sdCoreChecks;
        private AppDomain pluginAppDomain;

        public Service()
		{
			logger.Trace("start ctor");
			timer = new Timer(ConfigurationSingleton.Instance.Config.CheckInterval*1000);
			timer.Elapsed += Timer_Elapsed;

			ICheckTypeList = new List<string>();
            sliceCheckList = new List<string>();
            

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
	    InstanceProxy instanceProxy = null;
        private void CreateBaseChecks()
		{
			logger.Info("Creating Base Checks...");

			sdCoreChecks.Add(CreateCheck<NetworkTrafficCheck>());
			sdCoreChecks.Add(CreateCheck<DriveInfoBasedDiskUsageCheck>());
			sdCoreChecks.Add(CreateCheck<ProcessorCheck>());
			sdCoreChecks.Add(CreateCheck<ProcessCheck>());
			sdCoreChecks.Add(CreateCheck<PhysicalMemoryFreeCheck>());
			sdCoreChecks.Add(CreateCheck<PhysicalMemoryUsedCheck>());
			sdCoreChecks.Add(CreateCheck<PhysicalMemoryCachedCheck>());
			sdCoreChecks.Add(CreateCheck<SwapMemoryFreeCheck>());
			sdCoreChecks.Add(CreateCheck<SwapMemoryUsedCheck>());
			sdCoreChecks.Add(CreateCheck<IOCheck>());
			sdCoreChecks.Add(CreateCheck<IISCheck>());

			logger.Info("Base Check Creation Complete...");
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
			var pluginPath = Path.Combine(basePath, "plugins");

			if (!Directory.Exists(pluginPath))
			{
				return;
			}

            logger.Trace("Finding plugins");

            var di = new DirectoryInfo(pluginPath);
			var fileInfo = di.GetFiles("*.dll");
		    

            if (fileInfo.Any())
		    {
		        pluginAppDomain = AppDomain.CreateDomain("TetherPlugins", null, new AppDomainSetup
		        {
		            ApplicationBase = pluginPath,
		            PrivateBinPath = pluginPath,
		            PrivateBinPathProbe = pluginPath,

		        });
		        
		        instanceProxy = pluginAppDomain.CreateInstanceFromAndUnwrap(typeof(Service).Assembly.Location, typeof(InstanceProxy).FullName) as InstanceProxy;
		    }

		    if (instanceProxy == null)
		    {
		        logger.Warn("Instance Proxy is null, no plugins will be loaded.");
		        return;
		    }

            foreach (var info in fileInfo)
			{
				try
				{
                    var def = AssemblyDefinition.ReadAssembly(info.FullName);

				    var it = def.MainModule.Types.Where(e => e.Interfaces.Any(r => (r.FullName == typeof(ICheck).FullName)) ||  (e.Interfaces.Any(r => r.FullName == typeof(ILongRunningCheck).FullName))).ToList();
                    
                    var isPlugin = false;

				    if (it.Any())
				    {
				        var res = instanceProxy.LoadLibrary(info.FullName);
				        ICheckTypeList.Add(res);
				        isPlugin = true;
                    }

				    var typeDefinitions = def.MainModule.Types.Where(f=> f.CustomAttributes.Any(a=>a.AttributeType.FullName == typeof(PerformanceCounterGroupingAttribute).FullName )  ).ToList();

				    if (typeDefinitions.Any())
				    {
				        logger.Trace($"Found slice {info.FullName}");

				        var loadSlices = instanceProxy.LoadSlices(info.FullName);

				        sliceCheckList.Add(loadSlices);

				        isPlugin = true;
                    }

                    
                    if (isPlugin)
                    {
                        logger.Debug("Loaded plugin " + info);
                    }
                }
				catch (Exception e)
				{
					logger.Warn(e, $"Unable to load {info.FullName}");
				}
			}

            var checkNames = ICheckTypeList.Select(e => e.GetType().FullName).ToList();

            foreach (var JsonFiles in di.GetFiles("*.json"))
            {
                if (checkNames.Contains(Path.GetFileNameWithoutExtension(JsonFiles.Name)))
                {
                    instanceProxy.PluginSettings.Add(Path.GetFileNameWithoutExtension(JsonFiles.Name), JObject.Parse(File.ReadAllText(JsonFiles.FullName)) as dynamic);
                }
            }

            ICheckTypeList = ICheckTypeList.Distinct().ToList();

            logger.Trace("Plugins found!");
		}
		//private static void DisposeAll(PerformanceCounter[] counters)
		//{
		//	foreach (var counter in counters)
		//	{
		//		try
		//		{
		//			counter.Dispose();
		//		}
		//		catch (Exception)
		//		{
		//			// Yeah, I know. Yeah, I really do know.
		//		}
		//	}
		//}

		

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			var results = new Dictionary<string, object>();
			List<dynamic> objList = new List<dynamic>();

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

		    try
		    {
		        logger.Info("Polling long checks");

		        var longRunningChecks = instanceProxy.GetLongRunningChecks();

		        if (longRunningChecks.Any())
		        {
		            foreach (var lrc in longRunningChecks)
		            {
		                results.Add(lrc.Item1, lrc.Item2);
		            }

		        }
		    }
		    catch (Exception exception)
		    {
		        logger.Warn(exception, "Error on polling for long checks");
		    }

		    var pluginCollection = new Dictionary<string, object>();
			logger.Info("Polling Slices");
			Parallel.ForEach(
				ICheckTypeList,
				check =>
				{

					logger.Debug("{0}: start", check);
					try
					{
					    //if (typeof(IRequireConfigurationData).IsInstanceOfType(check) && PluginSettings.ContainsKey( check.GetType().FullName ))
					    //{
                        //  ((IRequireConfigurationData)check).LoadConfigurationData(PluginSettings[check.GetType().FullName]);
					    //}

					    var result = instanceProxy.PerformCheck(check);

						if (result == null)
						{
							return;
						}

                        if (pluginCollection.ContainsKey(check))
                        {
                            logger.Warn("Key already exists for plugin " + check + " of type " + check.GetType());
                            pluginCollection.Add(check + "2", result);
                        }
                        else
                        {
                            pluginCollection.Add(check, result);
                        }

                        logger.Debug($"{check}: end");
					}
					catch (Exception ex)
					{
						logger.Error(ex, $"Error on {check.GetType()}");
					}

				});

			logger.Info("Generating SD compatible names for slices.");

			Parallel.ForEach(
			    sliceCheckList,
				type =>
				{
					try
					{
					    var invokeres = instanceProxy.GetSlice(type);
					    foreach (var invokere in invokeres)
					    {
					        pluginCollection.Add(invokere.Key, JsonConvert.DeserializeObject(invokere.Value));
					    }
                    }
					catch (Exception exception)
					{
						logger.Error(exception, $"Error during slice {type}");
					}

				});

			results.Add("plugins", pluginCollection);

			try
			{
				var poster = new PayloadPoster(results);
				poster.Post();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Error with sending data to SD servers");
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