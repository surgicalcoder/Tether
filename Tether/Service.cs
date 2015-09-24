using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using NLog;
using NLog.Fluent;
using Tether.Plugins;
using Topshelf;
using Timer = System.Timers.Timer;

namespace Tether
{
    public class Service : ServiceControl
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Timer timer;
        Thread pluginDetectionThread;

        public Service()
        {
            timer = new Timer(ConfigurationSingleton.Instance.Config.CheckInterval*1000);
            timer.Elapsed += Timer_Elapsed;
            
            pluginDetectionThread = new Thread(DetectPlugins);
            pluginDetectionThread.Start();
        }

        private void DetectPlugins()
        {
            
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var results = new Dictionary<string, object>();
            var Checks = new List<ICheck>(); // TODO : REMOVE
            
                foreach (var check in Checks)
            {
                logger.Debug("{0}: start", check.GetType());
                try
                {
                    var result = check.DoCheck();
                    if (result != null)
                    {
                        // TODO: Something, something, plugin, metrics.
                        logger.Debug("{0}: end", check.GetType());
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            

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

        public bool Start(HostControl hostControl)
        {
            timer.Enabled = true;
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            if (pluginDetectionThread.IsAlive)
            {
                pluginDetectionThread.Abort();
            }

            timer.Enabled = false;
            return true;
        }
    }
}