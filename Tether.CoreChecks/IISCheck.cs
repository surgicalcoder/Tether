using System.Collections.Generic;
using NLog;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    public class IISCheck : PerformanceCounterBasedCheck, ICheck
    {
        #region ICheck Members

        public string Key
        {
            get { return "iisReqPerSec"; }
        }

        public override IDictionary<string, string> Names
        {
            get { return _names; }
        }

        public IISCheck() : base()
        {
            _names = new Dictionary<string, string>();
            _names.Add("Web Service", "Total Method Requests/sec");
        }

        public object DoCheck()
        {
            if (PerformanceCounter == null)
            {
                logger.Warn("Performance counter is null.");
                return null;
            }

            float requestsPerSecond = PerformanceCounter.NextValue();
            logger.Debug("IIS req/s is: {0}", requestsPerSecond);
            return string.Format("{0:0.00}", requestsPerSecond);
        }

        #endregion

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, string> _names;
    }
}