using System;
using System.IO;
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
            Config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("settings.json"));
        }
    }
}