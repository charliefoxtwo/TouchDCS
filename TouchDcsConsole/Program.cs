using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using TouchDcsWorker;

namespace TouchDcsConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await Worker.Run((b, logLevel) => b.SetMinimumLevel(logLevel).AddConsole().AddFile(string.Empty,
                options =>
                {
                    options.MinLevel = logLevel;
                    var fileName = $"log/TouchDCS_{DateTime.Now:yyyy-MM-ddTHHmmss}.log";
                    options.FormatLogFileName = _ => fileName;
                }));
        }
    }
}
