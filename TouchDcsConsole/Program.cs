using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TouchDcsWorker;

namespace TouchDcsConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Information).AddConsole());

            await Worker.Run(loggerFactory);
        }
    }
}