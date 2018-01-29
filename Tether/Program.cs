using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using NLog;
using Quartz;
using Topshelf;
using Topshelf.Quartz;

namespace Tether
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string pluginPath = Path.Combine(basePath, "plugins");


        static void Main(string[] args)
        {
            try
            {
                logger.Trace("Performing Host Init");


                AppDomain.MonitoringIsEnabled = true;

                var tempPath = Path.Combine(pluginPath, "_temp");
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

                if (!File.Exists(Path.Combine(pluginPath, "Tether.Plugins.dll")))
                {
                    File.Copy(Path.Combine(basePath, "Tether.Plugins.dll"), Path.Combine(pluginPath, "Tether.Plugins.dll"));
                    File.Copy(Path.Combine(basePath, "Tether.Plugins.pdb"), Path.Combine(pluginPath, "Tether.Plugins.pdb"));
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

                        if (!Config.ConfigurationSingleton.Instance.Config.DisableResending)
                        {
                            service.ScheduleQuartzJob(b => b.WithJob(() => JobBuilder.Create<ResenderJob>().Build())
                            .AddTrigger(() => TriggerBuilder.Create()
                                .WithSimpleSchedule(builder => builder.WithMisfireHandlingInstructionFireNow()
                                    .WithIntervalInSeconds(Config.ConfigurationSingleton.Instance.Config.RetriesResendInterval)
                                    .RepeatForever())
                                .Build()));
                        }

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
