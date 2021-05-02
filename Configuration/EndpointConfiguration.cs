using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Newtonsoft.Json;

namespace Configuration
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EndpointConfiguration
    {
        public string IpAddress { get; set; } = null!;

        [JsonIgnore]
        public IPAddress Ip => IPAddress.Parse(IpAddress);

        [Range(1, 65535)]
        public int SendPort { get; set; }
        [Range(1, 65535)]
        public int ReceivePort { get; set; }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        protected bool Equals(EndpointConfiguration other)
        {
            return IpAddress == other.IpAddress && SendPort == other.SendPort && ReceivePort == other.ReceivePort;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IpAddress, SendPort, ReceivePort);
        }
    }
}