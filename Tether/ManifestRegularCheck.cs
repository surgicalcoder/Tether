using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;
using Quartz;
using SharpCompress.Archive;
using SharpCompress.Archive.Zip;
using SharpCompress.Common;
using Tether.Config;

namespace Tether
{
    [PersistJobDataAfterExecution]
    [DisallowConcurrentExecution]
    public class ManifestRegularCheck : IJob
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(ConfigurationSingleton.Instance.Config.PluginManifestLocation))
                {
                    return;
                }
                var pluginPath = Path.Combine(basePath, "plugins");
                var tempPluginPath = Path.Combine(pluginPath, "_temp");

                string contents;
                var client = new WebClient();
                if (ConfigurationSingleton.Instance.Config.PluginManifestLocation.StartsWith("http"))
                {
                    contents = client.DownloadString(ConfigurationSingleton.Instance.Config.PluginManifestLocation);
                }
                else
                {
                    string localPath = ConfigurationSingleton.Instance.Config.PluginManifestLocation;
                    if (localPath.StartsWith("~"))
                    {
                        localPath = Path.Combine(basePath, localPath.Substring(2));
                    }

                    logger.Debug($"Reading Plugin Manifest from {localPath}");

                    contents = File.ReadAllText(localPath);
                }

                var manifest = JsonConvert.DeserializeObject<PluginManifest>(contents);

                if (!manifest.Items.Any())
                {
                    logger.Debug("No items found");
                    return;
                }

                bool requiresServiceRestart = false;
                
                foreach (var manifestItem in manifest.Items.Where(f => new Regex(f.MachineFilter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).IsMatch(Environment.MachineName)))
                {
                    var assembly = ConfigurationSingleton.Instance.PluginAssemblies.FirstOrDefault(e => e.GetName().Name == manifestItem.PluginName);

                    if (assembly != null)
                    {
                        if (assembly.GetName().Version.ToString() != manifestItem.PluginVersion)
                        {
                            logger.Debug($"Assembly: {assembly.FullName}, Current assembly version = {assembly.GetName().Version}, expecting {manifestItem.PluginVersion}");

                            var zipPath = Path.Combine(tempPluginPath, assembly.GetName().Name + ".zip");

                            if (!Directory.Exists(tempPluginPath))
                            {
                                Directory.CreateDirectory(tempPluginPath);
                            }

                            client.DownloadFile(manifestItem.PluginDownloadLocation, zipPath);

                            Unzip(zipPath, tempPluginPath);

                            File.Delete(zipPath);

                            requiresServiceRestart = true;
                        }
                    }
                    else
                    {
                        logger.Debug($"Assembly not found: {manifestItem.PluginName}, downloading from {manifestItem.PluginDownloadLocation}");

                        var zipPath = Path.Combine(tempPluginPath, manifestItem.PluginName + ".zip");

                        if (!Directory.Exists(tempPluginPath))
                        {
                            Directory.CreateDirectory(tempPluginPath);
                        }

                        client.DownloadFile(manifestItem.PluginDownloadLocation, zipPath);

                        Unzip(zipPath, tempPluginPath);

                        File.Delete(zipPath);

                        requiresServiceRestart = true;
                    }
                }

                if (requiresServiceRestart)
                {

                    var strCmdText = "/C net stop Tether & net start Tether";
                    var info = new ProcessStartInfo("CMD.exe", strCmdText)
                    {
                        WorkingDirectory = pluginPath
                    };

                    logger.Fatal("!!! GOING DOWN FOR AN UPDATE TO PLUGINS !!!");

                    Process.Start(info);
                }

            }
            catch (Exception e)
            {
                logger.Warn(e, "Error while checking Manifests");
            }
        }


        private void Unzip(string zipFileName, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            ExtractZipFile(zipFileName, destinationPath);

        }

        private void ExtractZipFile(string filePath, string destination)
        {
            logger.Info($"Unzipping '{filePath}' to {destination}");

            using (var archive = ZipArchive.Open(new FileInfo(filePath)))
            {
                archive.WriteToDirectory(destination, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }
        }
    }
}