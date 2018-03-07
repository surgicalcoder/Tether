using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using NLog;
using Tether.Plugins;

namespace Tether.Metrics
{
    public class NetworkTrafficMetricProvider : IMetricProvider
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public List<Metric> GetMetrics()
        {
            var values = new List<Metric>();

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var nic in interfaces.Where(f=>f.OperationalStatus == OperationalStatus.Up && !f.Name.ToLowerInvariant().Contains("pseudo") && !f.Name.ToLowerInvariant().Contains("teredo") ))
            {
                var stats = nic.GetIPv4Statistics();

                var rx = stats.BytesReceived;
                var tx = stats.BytesSent;

                if (delta.ContainsKey(nic.Name) && delta[nic.Name] is Tuple<long, long> tup)
                {
                    rx = rx - tup.Item1;
                    tx = tx - tup.Item2;
                    
                    delta.Remove(nic.Name);
                }

                if (rx < 0)
                {
                    rx = 0;
                }

                if (tx < 0)
                {
                    tx = 0;
                }

                values.Add(new Metric("system.net.bytes_sent", tx, tags: new Dictionary<string, string>{{"device_name",nic.Name}}));
                values.Add(new Metric("system.net.bytes_rcvd", rx, tags: new Dictionary<string, string>{{"device_name",nic.Name}}));

                delta.Add(nic.Name, new Tuple<long, long>(stats.BytesReceived,stats.BytesSent));
            }

            return values;
        }

        private static Dictionary<string, Tuple<long, long>> delta = new Dictionary<string, Tuple<long, long>>();
    }
}
