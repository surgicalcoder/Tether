using System;
using System.Threading;
using System.Timers;
using NLog;
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
            throw new NotImplementedException();
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