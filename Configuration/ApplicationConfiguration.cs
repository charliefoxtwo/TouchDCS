using System.Collections.Generic;
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
        public Dictionary<string, HashSet<string>>? Aliases { get; set; }
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
            config.Aliases = null;

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

            if (otherConfiguration.Aliases != null)
            {
                if (Aliases is null)
                {
                    Aliases = otherConfiguration.Aliases;
                }
                else
                {
                    foreach (var (key, aliases) in otherConfiguration.Aliases)
                    {
                        if (!Aliases.TryGetValue(key, out var existing))
                        {
                            existing = new HashSet<string>();
                        }

                        foreach (var alias in aliases)
                        {
                            existing.Add(alias);
                        }

                        Aliases[key] = existing;
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
                Aliases = new Dictionary<string, HashSet<string>>
                {
                    ["A-10C"] = new() { "A-10C_2" },
                    ["AH-6J"] = new() { "AH-6", "BlackHawk" },
                    ["C-101CC"] = new() { "C-101EB" },
                    ["F-14B"] = new() { "F-14A-135-GR" },
                    ["FA-18C_hornet"] = new() { "EA-18G", "FA-18E", "FA-18F" },
                    // this is a catch-all for all non-clickable-cockpits (everything from https://github.com/DCSFlightpanels/dcs-bios/blob/master/Scripts/DCS-BIOS/lib/AircraftList.lua with *false*)
                    ["FC3"] = new() { "A-10A", "F-15C", "F-16A", "J-11A", "MiG-29A", "MiG-29G", "MiG-29S", "Su-25", "Su-25T", "Su-27", "Su-33", "AC_130", "Cessna_210N", "DC3", "F-117A", "F-2A", "F-2B", "F4e", "FA_18D", "Flyer1", "J-20A", "Mig-23UB", "MirageF1", "MirageF1CT", "MQ9_PREDATOR", "Rafale_A_S", "Rafale_B", "Rafale_C", "Rafale_M", "REISEN52", "RST_Eurofighter", "RST_Eurofighter_AG", "Su-30M", "Su-30MK", "Su-30SM", "Su-57", "Super_Etendard", "T-4", "VSN_AJS37Viggen", "VSN_C17A", "VSN_C5_Galaxy", "VSN_E2D", "VSN_Eurofighter", "VSN_Eurofighter_AG", "VSN_F104G", "VSN_F104G_AG", "VSN_F105D", "VSN_F105G", "VSN_F14A", "VSN_F14B", "VSN_F15E", "VSN_F15E_AA", "VSN_F16A", "VSN_F16AMLU", "VSN_F16CBL50", "VSN_F16CBL52D", "VSN_F16CMBL50", "VSN_F22", "VSN_F35A", "VSN_F35A_AG", "VSN_F35B", "VSN_F35B_AG", "VSN_F4E", "VSN_F4E_AG", "VSN_F5E", "VSN_F5N", "VSN_FA18C", "VSN_FA18C_AG", "VSN_FA18C_Lot20", "VSN_FA18F", "VSN_FA18F_AG", "VSN_Harrier", "VSN_M2000", "VSN_P3C", "VSN_TornadoGR4", "VSN_TornadoIDS", "VSN_Su47", "VSN_UFO" },
                    ["L-39ZA"] = new() { "L-39C" },
                    ["P-47D"] = new() { "P-47D-30", "P-47D-30b11", "P-47D-40" },
                    ["P-51D"] = new() { "TF-51D", "P-51D-30-NA" },
                    ["SA342M"] = new() { "SA342Minigun", "SA342Mistral", "SA342L" },
                    ["SpitfireLFMkIX"] = new() { "SpitfireLFMkIXCW" },
                    ["UH-1H"] = new() { "Bell47_2" },
                    ["VNAO_Room"] = new() { "VNAO_Ready_Room" },
                    ["VNAO_T-45"] = new() { "T-45" },
                },
                DcsBios = new DcsBiosConfiguration
                {
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