using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DcsBios.Communicator;
using DcsBios.Communicator.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OscCommunicator;
using TouchDcsWorker;

namespace TouchDcsTest.TranslatorTests
{
    public abstract class TranslatorTestBase
    {
        private const string IpAddress = "1.1.1.1";
        protected ToBiosVerifier BiosVerifier { get; }
        protected ToOscVerifier OscVerifier { get; }

        protected TranslatorTestBase(string moduleName)
        {
            var logger = LoggerFactory.Create(c => c.SetMinimumLevel(LogLevel.Debug)).CreateLogger<BiosOscTranslator>();
            var oscSender = new Mock<IOscSendClient>();
            oscSender.SetupGet(c => c.DeviceIpAddress).Returns(IpAddress);
            var biosSender = new Mock<IBiosSendClient>();
            var translator = new BiosOscTranslator(new List<IOscSendClient> { oscSender.Object },
                biosSender.Object, BuildBiosConfigurations(), new HashSet<string>(),
                null, logger);

            BiosVerifier = new ToBiosVerifier(translator, biosSender);
            OscVerifier = new ToOscVerifier(translator, oscSender);

            translator.FromBios(BiosListener.AircraftNameBiosCode, moduleName);
        }

        private static IEnumerable<AircraftBiosConfiguration> BuildBiosConfigurations()
        {
            return Task.WhenAll(Directory.EnumerateFiles(@"Resources/BiosConfigurations", "*.json", SearchOption.AllDirectories)
                .Select(async f => await AircraftBiosConfiguration.BuildFromConfiguration(new FileInfo(f)))).Result;
        }

        protected class ToOscVerifier
        {
            private readonly IBiosTranslator _translator;
            private readonly Mock<IOscSendClient> _oscSender;

            public ToOscVerifier(IBiosTranslator translator, Mock<IOscSendClient> oscSender) =>
                (_translator, _oscSender) = (translator, oscSender);

            public void Exactly(string key, int value)
            {
                _translator.FromBios(key, value);
                _oscSender.Verify(s => s.Send(OscCodeFor(key), value));
            }

            public void Exactly(string key, string value)
            {
                _translator.FromBios(key, value);
                _oscSender.Verify(s => s.Send(OscCodeFor(key), value));
            }

            private static string OscCodeFor(in string address) => $"/{address}";
        }

        protected class ToBiosVerifier
        {
            private readonly IOscTranslator _translator;
            private readonly Mock<IBiosSendClient> _biosSender;

            public ToBiosVerifier(IOscTranslator translator, Mock<IBiosSendClient> biosSender) =>
                (_translator, _biosSender) = (translator, biosSender);

            public void Exactly(string address, int value)
            {
                Exactly(address, value.ToString(), value);
            }

            public void Exactly(string address, string value)
            {
                Exactly(address, value, value);
            }

            public void Exactly<TValue>(string address, string expected, TValue value)
            {
                _translator.FromOsc(IpAddress, address, value);
                _biosSender.Verify(s => s.Send(address, expected));
            }
        }
    }
}