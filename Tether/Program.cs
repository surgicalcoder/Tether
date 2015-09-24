using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NLog;

namespace Tether
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            logger.Trace("Test");

        }
    }
}
