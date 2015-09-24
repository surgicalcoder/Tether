using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tether.Plugins
{
    public interface ICheck
    {
        /// <summary>
        /// Gets the key name for the check.
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Performs the check.
        /// </summary>
        object DoCheck();
    }
}
