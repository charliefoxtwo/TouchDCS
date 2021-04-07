using System.Collections.Generic;

namespace Configuration
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class OscConfiguration
    {
        public List<EndpointConfiguration> Devices { get; set; } = null!;
    }
}