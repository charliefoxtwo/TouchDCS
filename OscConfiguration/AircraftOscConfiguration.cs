using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OscConfiguration
{
    /// <summary>
    /// Dictionary of OSC Address -> <see cref="OscControl"/>
    /// </summary>
    public class AircraftOscConfiguration
    {
        public string SyncAddress { get; set; } = null!;
        public Dictionary<string, OscControl> Properties { get; set; } = new ();

        public static AircraftOscConfiguration BuildFromFile(FileInfo configFile)
        {
            var json = File.ReadAllText(configFile.FullName);
            return JsonConvert.DeserializeObject<AircraftOscConfiguration>(json);
        }
    }
}