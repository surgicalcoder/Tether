using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tether.Plugins
{
    public class Metric
    {
        public Metric()
        {
            Timestamp = DateTime.UtcNow;
        }

        public Metric(string name, DateTime timestamp, float value, MetricType type, string hostname,
            Dictionary<string, string> tags)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Timestamp = timestamp;
            Value = value;
            Type = type;
            Hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
            Tags = tags;
        }

        public Metric(string name, float value, MetricType type = MetricType.Gauge, Dictionary<string, string> tags=null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            Type = type;
            Tags = tags;
            Hostname = Environment.MachineName;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"{Name}: {Value}, {nameof(Tags)}: {Tags?.Count}";
        }

        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
        public MetricType Type { get; set; }
        public string Hostname { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
