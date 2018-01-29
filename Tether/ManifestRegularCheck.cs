using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Mono.Cecil;
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
                //var tempPluginPath = Path.Combine(pluginPath, "_temp");

                var contents = string.Empty;

                var client = new WebClient();

                Uri newUri;

                if (Uri.TryCreate(ConfigurationSingleton.Instance.Config.PluginManifestLocation, UriKind.Absolute, out newUri))
                {
                    if (newUri.Scheme == "http" || newUri.Scheme == "https")
                    {
                        contents = client.DownloadString(ConfigurationSingleton.Instance.Config.PluginManifestLocation);
                    }
                    else if (newUri.Scheme == "dns")
                    {
                        throw new NotImplementedException("DNS scheme is not implemented yet");
                    }
                    else
                    {
                        throw new ArgumentException($"Scheme ${newUri.Scheme} is not supported");
                    }
                }
                else
                {
                    var localPath = ConfigurationSingleton.Instance.Config.PluginManifestLocation;

                    if (localPath.StartsWith("~"))
                    {
                        localPath = Path.Combine(basePath, localPath.Substring(2));
                    }

                    logger.Debug($"Reading Plugin Manifest from {localPath}");

                    contents = File.ReadAllText(localPath);
                }

                if (string.IsNullOrWhiteSpace(contents))
                {
                    return;
                }
                
                var manifest = JsonConvert.DeserializeObject<PluginManifest>(contents);

                if (!manifest.Items.Any())
                {
                    logger.Debug("No items found");
                    return;
                }

                foreach (var manifestItem in manifest.Items.Where(f => new Regex(f.MachineFilter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).IsMatch(Environment.MachineName)))
                {
                    var assembly = ConfigurationSingleton.Instance.PluginAssemblies.FirstOrDefault(f => f.Name == manifestItem.PluginName);

                    if (assembly != null)
                    {
                        if (assembly.Version.ToString() == manifestItem.PluginVersion)
                        {
                            continue;
                        }

                        logger.Debug($"Assembly: {assembly.FullName}, Current assembly version = {assembly.Version}, expecting {manifestItem.PluginVersion}");

                        DownloadAndExtract(pluginPath, client, manifestItem);
                    }
                    else
                    {
                        logger.Debug($"Assembly not found: {manifestItem.PluginName}, downloading from {manifestItem.PluginDownloadLocation}");

                        DownloadAndExtract(pluginPath, client, manifestItem);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Warn(e, "Error while checking Manifests");
            }
        }

        private void DownloadAndExtract(string pluginPath, WebClient client, PluginManifestItem manifestItem)
        {
            var zipPath = Path.Combine(pluginPath, manifestItem.PluginName + ".zip");

            Directory.CreateDirectory(pluginPath);

            client.DownloadFile(manifestItem.PluginDownloadLocation, zipPath);

            Unzip(zipPath, pluginPath);

            File.Delete(zipPath);
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