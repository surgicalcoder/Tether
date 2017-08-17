namespace Tether.Config
{
    public class Configuration
    {
        public Configuration()
        {
            CheckInterval = 60;
            ManifestCheckInterval = 300;
            RetriesResendInterval = 300;
            RetriesCount = 5;
        }


        public string ServerDensityUrl { get; set; }
        public string ServerDensityKey { get; set; }
        public int CheckInterval { get; set; }
        public int ManifestCheckInterval { get; set; }
        public string PluginManifestLocation { get; set; }       

        public int RetriesCount { get; set; }
        public int RetriesResendInterval { get; set; }
    }
}