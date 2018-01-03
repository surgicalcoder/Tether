using System;

namespace Tether.Plugins
{
    public interface ILongRunningCheck
    {
        /// <summary>
        /// Gets the key name for the check.
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Performs the check.
        /// </summary>
        object DoCheck();

        TimeSpan CacheDuration { get; }
    }
}