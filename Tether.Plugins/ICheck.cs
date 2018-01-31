using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tether.Plugins
{

    public interface IPluginCheck
    {
        List<Metric> GetMetrics();
    }


    public interface ILongRunningPluginCheck
    {
        List<Metric> GetMetrics();

        TimeSpan CacheDuration { get; }
    }

    public class Metric
    {
        public Metric()
        {
        }

        public Metric(string name, DateTime timestamp, float value, MetricType type, string hostname,
            Dictionary<string, string> tags)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Timestamp = timestamp;
            Value = value;
            Type = type;
            Hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }

        public Metric(string name, float value, MetricType type = MetricType.Gague, Dictionary<string, string> tags=null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            Type = type;
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
            Hostname = Environment.MachineName;
        }

        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
        public MetricType Type { get; set; }
        public string Hostname { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public enum MetricType
    {
        Gague,
        Increment,
        Decrement,
        Histogram,
        Rate,
        Count,
        MonotonicCount
    }

}
