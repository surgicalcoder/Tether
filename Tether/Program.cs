using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NLog;
using Topshelf;

namespace Tether
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            try
            {
                logger.Trace("Performing Host Init");
                Host host = HostFactory.New(x =>
                {
                    x.UseNLog();
                    x.Service<Service>();
                    x.RunAsLocalSystem();
                    x.StartAutomaticallyDelayed();
                    x.SetDescription("ThreeOneThree.Tether");
                    x.SetDisplayName("ThreeOneThree.Tether");
                    x.SetServiceName("ThreeOneThree.Tether");
                });

                host.Run();
            }
            catch (Exception ex)
            {
                logger.FatalException("Problem when trying to run host", ex);
            }

        }
    }
}
