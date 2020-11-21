using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Configuration
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EndpointConfiguration
    {
        public string IpAddress { get; set; } = null!;

        public IPAddress Ip => IPAddress.Parse(IpAddress);

        [Range(1, 65535)]
        public int SendPort { get; set; }
        [Range(1, 65535)]
        public int ReceivePort { get; set; }
    }
}