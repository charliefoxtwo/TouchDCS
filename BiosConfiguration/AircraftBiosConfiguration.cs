using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BiosConfiguration
{
    public class AircraftBiosConfiguration : Dictionary<string, BiosCategory>
    {
        public string AircraftName { get; private set; } = null!;

        public static async Task<AircraftBiosConfiguration> BuildFromConfiguration(FileInfo configFile)
        {
            var fileData = await File.ReadAllTextAsync(configFile.FullName);
            var dcsConfiguration = JsonConvert.DeserializeObject<AircraftBiosConfiguration>(fileData, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                }
            });
            if (dcsConfiguration is null) throw new ConfigException(configFile.FullName, "Unable to parse config file.");

            dcsConfiguration.AircraftName = Path.GetFileNameWithoutExtension(configFile.FullName);

            return dcsConfiguration;
        }
    }
}