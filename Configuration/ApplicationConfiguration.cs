﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        public HashSet<string>? CommonModules { get; set; }
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
                var baseConfiguration = DefaultConfiguration();

                var configPath = PathHelpers.FullOrRelativePath(ConfigLocation);
                if (!File.Exists(configPath))
                {
                    return null;
                }

                var builder = new ConfigurationBuilder().AddJsonFile(PathHelpers.FullOrRelativePath(ConfigLocation));
                baseConfiguration.MergeWith(builder.Build().Get<ApplicationConfiguration>());
                _configuration = baseConfiguration;
            }

            return _configuration;
        }

        public static void CreateNewConfiguration()
        {
            var config = DefaultConfiguration();
            config.Osc.Devices.Add(new EndpointConfiguration
            {
                IpAddress = "YOUR TABLET IP HERE",
                SendPort = 9000,
                ReceivePort = 8000,
            });
            config.CommonModules = null;
            config.DcsBios.Export = null;

            using var fs = File.Create(PathHelpers.FullOrRelativePath(ConfigLocation));
            using var sw = new StreamWriter(fs);
            sw.Write(JsonConvert.SerializeObject(config, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            }));
        }

        private void MergeWith(ApplicationConfiguration otherConfiguration)
        {
            // overwrite alias file name
            if (otherConfiguration.DcsBios.AliasFileName != null)
            {
                DcsBios.AliasFileName = otherConfiguration.DcsBios.AliasFileName;
            }

            // just overwrite the config locations
            if (otherConfiguration.DcsBios.ConfigLocations.Any())
            {
                DcsBios.ConfigLocations = otherConfiguration.DcsBios.ConfigLocations;
            }

            // overwrite the bios export settings
            if (otherConfiguration.DcsBios.Export != null)
            {
                DcsBios.Export = otherConfiguration.DcsBios.Export;
            }

            // merge devices
            foreach (var device in otherConfiguration.Osc.Devices)
            {
                Osc.Devices.Add(device);
            }

            // merge common modules
            if (otherConfiguration.CommonModules != null)
            {
                if (CommonModules is null)
                {
                    CommonModules = otherConfiguration.CommonModules;
                }
                else
                {
                    foreach (var module in otherConfiguration.CommonModules)
                    {
                        CommonModules.Add(module);
                    }
                }
            }

            // just overwrite the log level
            LogLevel = otherConfiguration.LogLevel;
        }

        private static ApplicationConfiguration DefaultConfiguration()
        {
            return new ApplicationConfiguration
            {
                Schema = "https://raw.githubusercontent.com/charliefoxtwo/TouchDCS/main/Configuration/Schema/ApplicationConfiguration.json.schema",
                LogLevel = LogLevel.Information,
                CommonModules = new HashSet<string>
                {
                    "CommonData",
                    "MetadataStart",
                    "MetadataEnd",
                    "NS430",
                },
                DcsBios = new DcsBiosConfiguration
                {
                    AliasFileName = "AircraftAliases.json",
                    ConfigLocations = new HashSet<string>
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
                    Devices = new HashSet<EndpointConfiguration>()
                }
            };
        }
    }
}