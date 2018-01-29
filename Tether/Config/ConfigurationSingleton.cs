using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Newtonsoft.Json;
using NLog;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Tether.Config
{
    public sealed class ConfigurationSingleton
    {
        private static readonly Lazy<ConfigurationSingleton> lazy = new Lazy<ConfigurationSingleton>(() => new ConfigurationSingleton());

        public Configuration Config  { get; set; }
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static ConfigurationSingleton Instance => lazy.Value;

        private ConfigurationSingleton()
        {
            var configPath = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),  "settings.json");

            var fileContents = File.ReadAllText(configPath);

            Config = JsonConvert.DeserializeObject<Configuration>(fileContents, new JsonSerializerSettings()
            {
                Error = delegate(object sender, ErrorEventArgs args)
                {
                    logger.Warn("Configuration Error: " + args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            });

            PluginAssemblies = new List<AssemblyNameDefinition>();
        }

        public List<AssemblyNameDefinition> PluginAssemblies { get; set; }

    }
}