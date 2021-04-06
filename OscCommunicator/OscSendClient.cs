using System;
using System.Net;
using Core.Logging;
using Rug.Osc;

namespace OscCommunicator
{
    public class OscSendClient : IOscSendClient, IDisposable
    {
        private readonly OscSender _sender;
        private readonly ILogger _log;
        public string DeviceIpAddress { get; }

        public OscSendClient(in IPAddress deviceIpAddress, in int sendPort, in ILogger logger)
        {
            _sender = new OscSender(deviceIpAddress, sendPort);
            _log = logger;
            DeviceIpAddress = deviceIpAddress.ToString();
        }

        public void Send(string address, object data)
        {
            if (address == "/UHF_MODE") _log.Debug($"{address} -> sending data -> {data}");
            var message = new OscMessage(address, data);
            _sender.Send(message);
        }

        public void Connect()
        {
            _sender.Connect();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _sender.Dispose();
        }
    }
}