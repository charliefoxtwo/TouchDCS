using System.Collections.Generic;

namespace Configuration
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class OscConfiguration
    {
        public List<string> ConfigLocations { get; set; } = null!;
        public List<EndpointConfiguration> Devices { get; set; } = null!;
    }
}