using System;
using System.Net;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Attempts to send OSC data to the specified address.
        /// </summary>
        /// <param name="address">The address to which data should be sent.</param>
        /// <param name="data">The data to send to the address.</param>
        /// <returns>Boolean value indicating success. False indicates an exception was thrown.</returns>
        public bool Send(string address, object data)
        {
            OscMessage message;

            try
            {
                message = new OscMessage(address, data);
            }
            catch (ArgumentException ex)
            {
                _log.LogError("Exception encountered while attempting to send {Data} to {Address}: {Message}", data,
                    address, ex.Message);
                return false;
            }

            _sender.Send(message);
            return true;
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