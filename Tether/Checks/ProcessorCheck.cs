using System;
using System.Collections.Generic;
using System.Timers;
using NLog;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    /// <summary>
    /// Class for checking processor usage.
    /// </summary>
    /// <remarks>
    /// This is not really the same as CPU "load" on Linux/Unix, but
    /// rather CPU usage.  I think Windows users know the difference, 
    /// but we'll probably need to document the subtleties.
    /// </remarks>
    public class ProcessorCheck : PerformanceCounterBasedCheck, ICheck
    {
        #region ICheck Members

        public string Key => "loadAvrg";

        public override IDictionary<string, string> Names => _names;

        public ProcessorCheck() : base()
        {
            _names = new Dictionary<string, string>
            {
                {"Processor", "% Processor Time"}
            };

            try
            {
                _values = new List<float>();
                _timer = new Timer(60000);

                _timer.Elapsed += Timer_Elapsed;
                _timer.Enabled = true;
                _timer.Start();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {

            if (PerformanceCounter == null)
            {
                return;
            }

            try
            {
                // stop the timer before doing this
                // agent-205
                _timer.Stop();

                float usage = PerformanceCounter.NextValue();

                if (usage > _max)
                {
                    _max = usage;
                }

                if (usage < _min)
                {
                    _min = usage;
                }

                // lock added for agent-205
                // bug pattern followed: http://code.google.com/p/moq/issues/detail?id=249
                lock (_values)
                {
                    _values.Add(usage);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            finally
            {
                // start the timer again
                // agent-205
                _timer.Start();
            }
        }

        public object DoCheck()
        {

            if (PerformanceCounter == null)
            {
                logger.Warn("Performance counter is null.");
                return null;
            }

            float sum = 0;
            int count = _values.Count;

            foreach (float usage in _values)
            {
                sum += usage;
            }

            // Clear out old values.
            _values.Clear();
            _max = 0;
            _min = 0;

            if (count > 0)
            {
                return $"{sum / count:0.00}";
            }
            else
            {
                return $"{0:0.00}";
            }
        }

        #endregion

        private IList<float> _values;
        private float _max;
        private float _min;
        private System.Timers.Timer _timer;
        private const int ProcessorInterval = 10 * 1000; // 10 seconds.
        private readonly IDictionary<string, string> _names = new Dictionary<string, string>();
        private static Logger logger = LogManager.GetCurrentClassLogger();
    }
}