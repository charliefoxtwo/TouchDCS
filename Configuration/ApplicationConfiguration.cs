using System.Collections.Generic;
using System.IO;
using Core;
using Core.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;


namespace Configuration
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ApplicationConfiguration
    {
        [JsonProperty("$schema")] public string Schema { get; set; } = null!;
        public DcsBiosConfiguration DcsBios { get; set; } = null!;
        public OscConfiguration Osc { get; set; } = null!;
        public List<string> CommonModules { get; set; } = null!;

        public LogLevel LogLevel { get; set; }


        private const string ConfigLocation = @"config.json";

        private static ApplicationConfiguration? _configuration;

        /// <summary>
        /// Gets the application configuration, or returns null if one does not exist.
        /// </summary>
        public static ApplicationConfiguration? Get()
        {
            if (_configuration is null)
            {
                var configPath = PathHelpers.FullOrRelativePath(ConfigLocation);
                if (!File.Exists(configPath))
                {
                    return null;
                }

                var builder = new ConfigurationBuilder().AddJsonFile(PathHelpers.FullOrRelativePath(ConfigLocation));
                _configuration = builder.Build().Get<ApplicationConfiguration>();
            }

            return _configuration;
        }

        public static void CreateNewConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                Schema = "https://raw.githubusercontent.com/charliefoxtwo/TouchDCS/main/Configuration/Schema/ApplicationConfiguration.json.schema",
                LogLevel = LogLevel.Info,
                CommonModules = new List<string>
                {
                    "CommonData",
                    "MetadataStart",
                    "MetadataEnd",
                    "NS430",
                },
                DcsBios = new DcsBiosConfiguration
                {
                    ConfigLocations = new List<string>
                    {
                        @"%userprofile%/Saved Games/DCS.openbeta/Scripts/DCS-BIOS/doc/json/",
                        @"%appdata%/DCS-BIOS/control-reference/json/",
                    },
                    Export = new EndpointConfiguration
                    {
                        IpAddress = "239.255.50.10",
                        SendPort = 7778,
                        ReceivePort = 5010,
                    },
                },
                Osc = new OscConfiguration
                {
                    Devices = new List<EndpointConfiguration>
                    {
                        new EndpointConfiguration
                        {
                            IpAddress = "YOUR TABLET IP HERE",
                            SendPort = 9000,
                            ReceivePort = 8000,
                        }
                    }
                }
            };

            using var fs = File.Create(PathHelpers.FullOrRelativePath(ConfigLocation));
            using var sw = new StreamWriter(fs);
            sw.Write(JsonConvert.SerializeObject(config, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
            }));
        }
    }
}