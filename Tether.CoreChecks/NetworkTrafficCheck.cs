using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using NLog;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    public class NetworkTrafficCheck : ICheck
    {
        #region ICheck Members

        public string Key
        {
            get { return "networkTraffic"; }
        }

        public object DoCheck()
        {
            IDictionary<string, Dictionary<string, long>> results = new Dictionary<string, Dictionary<string, long>>();
            logger.Info("Hash code: {0}", _networkTrafficStore.GetHashCode());
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var nic in interfaces)
            {

                if (nic.OperationalStatus != OperationalStatus.Up || nic.Name.ToLower().Contains("pseudo") || nic.Name.ToLower().Contains("teredo"))
                {
                    continue;
                }

                var stats = nic.GetIPv4Statistics();
                long received = stats.BytesReceived;
                long sent = stats.BytesSent;
                var key = nic.Name;

                logger.Info("{0} - Received: {1}", key, received);
                logger.Info("{0} - Sent: {1}", key, sent);

                if (!_networkTrafficStore.ContainsKey(key))
                {
                    _networkTrafficStore.Add(key, new Dictionary<string, long>());
                    _networkTrafficStore[key]["recv_bytes"] = received;
                    _networkTrafficStore[key]["trans_bytes"] = sent;

                    if (!results.ContainsKey(key))
                    {
                        results.Add(key, new Dictionary<string, long>());
                        results[key]["recv_bytes"] = received;
                        results[key]["trans_bytes"] = sent;
                    }

                }
                else
                {

                    if (!results.ContainsKey(key))
                    {
                        results.Add(key, new Dictionary<string, long>());

                        // we need to check if these have overflowed
                        // AGENT-199
                        logger.Info("received: {0}", key + ": " + received.ToString());
                        logger.Info("Previous: {0}", key + ": " + _networkTrafficStore[key]["recv_bytes"].ToString());

                        var recv_overflow = this.CheckForOverflow("recv", _networkTrafficStore[key], received);
                        var trans_overflow = this.CheckForOverflow("trans", _networkTrafficStore[key], sent);

                        results[key]["recv_bytes"] = recv_overflow[0];
                        results[key]["trans_bytes"] = trans_overflow[0];

                        // Store now for calculation next time.
                        _networkTrafficStore[key]["recv_bytes"] = recv_overflow[1];
                        _networkTrafficStore[key]["trans_bytes"] = trans_overflow[1];
                    }

                }

            }
            return results;
        }

        #endregion

        /// <summary>
        /// Check if the value has overflowed and reset to 0 
        /// http://connect.microsoft.com/VisualStudio/feedback/details/734915/getipv4statistics-bytesreceived-and-bytessent
        /// AGENT-199
        /// Factored into a separate method for testing
        /// </summary>
        /// <param name="toCheck">string of parameter to look up (recv / trans)</param>
        /// <param name="store">Past results</param>
        /// <param name="currentValue">current value from the results</param>
        /// <returns>Value with overflow taken into account</returns>
        public List<long> CheckForOverflow(string toCheck, Dictionary<string, long> store, long currentValue)
        {
            // make up the strings we need to check in the dictionaries
            var bytesString = toCheck + "_bytes";

            // if the last was higher than our current, we've overflowed
            // overflow occurs at UInt32.MaxValue
            // so if we subtract that in this situation, we'll get the actual delta
            if (currentValue < store[bytesString])
            {

                store[bytesString] -= UInt32.MaxValue;
            }

            var values = new List<long>();

            // calculate the delta
            values.Add(currentValue - store[bytesString]);

            // we need the 'raw' value for the next time round
            values.Add(currentValue);

            return values;

        }

        private static Dictionary<string, Dictionary<string, long>> _networkTrafficStore = new Dictionary<string, Dictionary<string, long>>();
        private static Logger logger = LogManager.GetCurrentClassLogger();
    }
}