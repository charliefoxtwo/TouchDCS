using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TouchDcsConsole.Logging;
using TouchDcsWorker;

namespace TouchDcsConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    WindowsCustom.EnableWindowsAnsiSequences();
                }
                catch
                {
                    // suppress this exception, if it fails who cares.
                }
            }

            var log = new Acacia(nameof(TouchDcsWorker));
            await Worker.Run(log);
        }
    }
}