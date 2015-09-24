namespace Tether
{
    public class Configuration
    {
        public string ServerDensityUrl { get; set; }

        public string ServerDensityKey { get; set; }


        public int CheckInterval { get; set; }

        public bool IISStatus { get; set; }
        public string MongoDBConnectionString { get; set; }


        public bool MongoDBDBStats { get; set; }

        public bool MongoDBReplSet { get; set; }

        public bool SQLServerStatus { get; set; }

        public string SQLServerCustomPrefix { get; set; }

        
    }
}