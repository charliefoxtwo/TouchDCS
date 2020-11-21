using System.Collections.Generic;
using Core;
using Core.Logging;
using Microsoft.Extensions.Configuration;


namespace Configuration
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ApplicationConfiguration
    {
        public DcsBiosConfiguration DcsBios { get; set; } = null!;
        public OscConfiguration Osc { get; set; } = null!;
        public List<string> CommonModules { get; set; } = null!;

        public LogLevel LogLevel { get; set; }


        private const string ConfigLocation = @"config.json";

        private static ApplicationConfiguration? _configuration;

        public static ApplicationConfiguration Get()
        {
            if (_configuration is null)
            {
                var builder = new ConfigurationBuilder().AddJsonFile(PathHelpers.FullOrRelativePath(ConfigLocation));
                _configuration = builder.Build().Get<ApplicationConfiguration>();
            }

            return _configuration;
        }
    }
}