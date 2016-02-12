using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using NLog;

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
        public PayloadPoster(IDictionary<string, object> results)
        {
            _results = results;
            _results.Add("os", "windows");
            _results.Add("agentKey", ConfigurationSingleton.Instance.Config.ServerDensityKey);

            try
            {
                _results.Add("internalHostname", Environment.MachineName);
            }
            catch (InvalidOperationException)
            {
            }

            _results.Add("agentVersion", "tether-0.0.10");
        }

        /// <summary>
        /// Creates and sends the HTTP POST.
        /// </summary>
        public void Post()
        {
            var payload = JsonConvert.SerializeObject(_results);
            var hash = MD5Hash(payload);

            // TODO: this is for quick testing; we'll need to add proxy 
            //       settings, read the response, etc.
            using (var client = new WebClient())
            {
                var data = new NameValueCollection();
                data.Add("payload", payload);
                logger.Trace(payload);
                data.Add("hash", hash);
                var url = string.Format("{0}{1}postback/", ConfigurationSingleton.Instance.Config.ServerDensityUrl, ConfigurationSingleton.Instance.Config.ServerDensityUrl.EndsWith("/") ? "" : "/");
                logger.Info("Posting to {0}", url);

                if (HttpWebRequest.DefaultWebProxy != null)
                {
                    client.Proxy = HttpWebRequest.DefaultWebProxy;
                }

                byte[] response = client.UploadValues(url, "POST", data);
                string responseText = Encoding.ASCII.GetString(response);

                if (responseText != "OK" && responseText != "\"OK\"")
                {
                    logger.Error("URL {0} returned: {1}", url, responseText);
                }

                logger.Trace(responseText);
            }
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
}