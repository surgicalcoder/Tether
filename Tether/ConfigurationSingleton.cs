using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Tether
{
    public sealed class ConfigurationSingleton
    {
        private static readonly Lazy<ConfigurationSingleton> lazy = new Lazy<ConfigurationSingleton>(() => new ConfigurationSingleton());

        public Configuration Config  { get; set; }
        

        public static ConfigurationSingleton Instance => lazy.Value;

        private ConfigurationSingleton()
        {
            Config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText( Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),  "settings.json")));
            PluginAssemblies = new List<Assembly>();
        }

        public List<Assembly> PluginAssemblies { get; set; }
        
    }
}