using System.Collections.Generic;

namespace Configuration
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DcsBiosConfiguration
    {
        public HashSet<string> ConfigLocations { get; set; } = null!;
        public EndpointConfiguration? Export { get; set; }
    }
}