using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Tether.Config;
using Tether.Plugins;

namespace Tether
{
    /// <summary>
    /// Class to POST the agent payload data to the Server Density servers.
    /// </summary>
    public class PayloadPoster
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initialises a new instance of the PayloadPoster class with the 
        /// provided values.
        /// </summary>
        /// <param name="results">The payload dictionary.</param>
        public PayloadPoster(Dictionary<string, object> results)
        {
            _results = results;
            _results.Add("os", "windows");
            _results.Add("agentKey", ConfigurationSingleton.Instance.Config.ServerDensityKey);
            _results.Add("collection_timestamp", (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

            try
            {
                _results.Add("internalHostname", Environment.MachineName);
            } catch (InvalidOperationException) {}

            try
            {
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

                if (assemblyVersion.ToString() == "0.0.0.0")
                {
                    _results.Add("agentVersion", "tether-x");
                }
                else
                {
                    _results.Add("agentVersion", "tether-" + Assembly.GetExecutingAssembly().GetName().Version);
                }
            }
            catch (Exception e)
            {
                logger.Warn(e, "Error on setting assembly version");

                _results.Add("agentVersion", "tether-e");
            }
            
        }

        /// <summary>
        /// Creates and sends the HTTP POST.
        /// </summary>
        public void Post()
        {
            var payload = JsonConvert.SerializeObject(_results);
            var hash = MD5Hash(payload);

            var data = new Dictionary<string, string> {{"payload", payload}, {"hash", hash}};

            if (logger.IsTraceEnabled)
            {
                logger.Trace(payload);
            }

            TransmitValues(data);
        }

        public static bool TransmitValues(Dictionary<string, string> data, bool bypassSave = false)
        {
            bool successful = false;
            using (var client = new WebClient())
            {
                var url = $"{ConfigurationSingleton.Instance.Config.ServerDensityUrl}{(ConfigurationSingleton.Instance.Config.ServerDensityUrl.EndsWith("/") ? "" : "/")}postback/";

                logger.Info($"Posting to {url}");

                if (WebRequest.DefaultWebProxy != null)
                {
                    client.Proxy = WebRequest.DefaultWebProxy;
                }

                try
                {

                    var response = client.UploadString(url, "POST", JsonConvert.SerializeObject(data));

                    var responseText = response;

                    if (responseText != "OK" && responseText != "\"OK\"")
                    {
                        logger.Error($"URL {url} returned: {responseText}");

                        SavePayloadForRetransmission(data);
                    }
                    else
                    {
                        successful = true;
                    }

                    logger.Trace(responseText);
                }
                catch (Exception e)
                {
                    if (bypassSave)
                    {
                        return successful;
                    }
                    logger.Warn(e, "Error on TransmitValues");
                    SavePayloadForRetransmission(data);
                }
                
            }

            return successful;
        }

        private static void SavePayloadForRetransmission(Dictionary<string, string> data)
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var retransmitRootPath = Path.Combine(basePath, "_retransmit");
            var zeroPath = Path.Combine(retransmitRootPath, "0");

            Directory.CreateDirectory(retransmitRootPath);
            Directory.CreateDirectory(zeroPath);

            for (int i = 0; i < ConfigurationSingleton.Instance.Config.RetriesCount; i++)
            {
                Directory.CreateDirectory(Path.Combine(retransmitRootPath, i.ToString()));
            }

            File.WriteAllText(Path.Combine(zeroPath, DateTime.Now.ToString("O").Replace("+", "-").Replace(":", "") + ".json"),  JsonConvert.SerializeObject(data));
        }

        private static string MD5Hash(string input)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            StringBuilder s = new StringBuilder();

            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }

            return s.ToString();
        }

        private IDictionary<string, object> _results;
    }
    
    class MetricJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null || value.GetType() != typeof(Metric))
            {
                return;
            }

            var item = (Metric) value;

            var array = new JArray
            {
                item.Name, 
                item.Timestamp.GetUnixTimestamp(),
                item.Value,
                new JObject()
                {
                    {"hostname", item.Hostname},
                    {"type", item.Type.ToString().ToLowerInvariant()},
                    {"tags", JToken.FromObject(item.Tags.Select(f=>$"{f.Key}:{f.Value}"))}
                }
            };

            array.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => null;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Metric);
        }
    }

    static class Extensions
    {
        public static int GetUnixTimestamp(this DateTime dt) => (int)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        // (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds
    }
}