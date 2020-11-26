using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Logging;

namespace DcsBiosCommunicator
{
    public class BiosUdpClient : IUdpReceiveClient, ISendClient, IDisposable
    {
        private const string BlankBiosCommand = "\n";

        public string DeviceIpAddress { get; }

        private readonly UdpClient _client;
        private readonly IPAddress _ipAddress;
        private readonly IPEndPoint _target;
        private readonly ILogger _log;

        public BiosUdpClient(IPAddress ipAddress, int sendPort, int receivePort, in ILogger logger)
        {
            _log = logger;

            _ipAddress = ipAddress;

            _client = new UdpClient { ExclusiveAddressUse = false };

            // TODO: this should probably be loopback?
            IPEndPoint localEndpoint = new (IPAddress.Any, receivePort);

            _target = new IPEndPoint(IPAddress.Broadcast, sendPort);
            DeviceIpAddress = _target.Address.ToString();

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _client.Client.Bind(localEndpoint);
        }

        public async Task<UdpReceiveResult> ReceiveAsync()
        {
            return await _client.ReceiveAsync();
        }

        public async Task Send(string biosAddress, object data)
        {
            var message = $"{biosAddress} {data}{BlankBiosCommand}";
            _log.Debug($"Sending {{{message.TrimEnd('\n')}}} to DCS-BIOS.");
            var byteData = Encoding.UTF8.GetBytes(message);
            await _client.SendAsync(byteData, byteData.Length, _target);
        }

        public void OpenConnection()
        {
            _log.Debug("Opening connection to DCS-BIOS...");
            _client.JoinMulticastGroup(_ipAddress);
            _log.Info("Connection to DCS-BIOS opened.");
        }

        public void Close()
        {
            _log.Debug("Closing connection to DCS-BIOS...");
            _client.Close();
            _log.Info("Connection to DCS-BIOS closed.");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Close();
            _client.Dispose();
        }
    }

    public interface IUdpReceiveClient
    {
        Task<UdpReceiveResult> ReceiveAsync();
    }
}