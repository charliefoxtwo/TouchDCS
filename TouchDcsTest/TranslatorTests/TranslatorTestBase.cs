using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BiosConfiguration;
using DcsBiosCommunicator;
using Moq;
using NUnit.Framework;
using OscCommunicator;
using OscConfiguration;
using TouchDcs;
using Range = Moq.Range;

namespace TouchDcsTest.TranslatorTests
{
    public abstract class TranslatorTestBase<T> where T : ITranslatorTest, new()
    {
        private const string IpAddress = "1.1.1.1";
        protected ToBiosVerifier BiosVerifier { get; }
        protected ToOscVerifier OscVerifier { get; }

        protected T Keys { get; }

        protected TranslatorTestBase(string moduleName)
        {
            Keys = new T();
            var logger = new TestLogger();
            logger.OnDebug += Console.WriteLine;
            logger.OnInfo += Console.WriteLine;
            logger.OnWarn += Assert.Fail;
            logger.OnError += Assert.Fail;
            logger.OnFatal += Assert.Fail;
            var oscSender = new Mock<IOscSendClient>();
            oscSender.SetupGet(c => c.DeviceIpAddress).Returns(IpAddress);
            var biosSender = new Mock<IBiosSendClient>();
            var translator = new BiosOscTranslator(new List<IOscSendClient> { oscSender.Object },
                biosSender.Object, BuildBiosConfigurations(), BuildOscConfigurations(), new HashSet<string>(),
                logger);

            BiosVerifier = new ToBiosVerifier(translator, biosSender);
            OscVerifier = new ToOscVerifier(translator, oscSender);

            translator.FromBios(BiosListener.AircraftNameBiosCode, moduleName);
            translator.FromOsc(IpAddress, Keys.SyncAddress, 1);
        }

        private static IEnumerable<AircraftBiosConfiguration> BuildBiosConfigurations()
        {
            return Task.WhenAll(Directory.EnumerateFiles(@"Resources/BiosConfigurations", "*.json", SearchOption.AllDirectories)
                .Select(async f => await AircraftBiosConfiguration.BuildFromConfiguration(new FileInfo(f)))).Result;
        }

        private static IEnumerable<AircraftOscConfiguration> BuildOscConfigurations()
        {
            return Directory.EnumerateFiles(@"Resources/OscConfigurations", "*.json", SearchOption.AllDirectories).Select(f =>
                AircraftOscConfiguration.BuildFromFile(new FileInfo(f)));
        }

        protected class ToOscVerifier
        {
            private readonly IBiosTranslator _translator;
            private readonly Mock<IOscSendClient> _oscSender;

            public ToOscVerifier(IBiosTranslator translator, Mock<IOscSendClient> oscSender) =>
                (_translator, _oscSender) = (translator, oscSender);

            public void Exactly(string key, float expected, int data)
            {
                _translator.FromBios(key, data);
                _oscSender.Verify(s => s.Send(OscCodeFor(key), expected));
            }

            public void Exactly(string key, string expected, string data)
            {
                _translator.FromBios(key, data);
                _oscSender.Verify(s => s.Send(OscCodeFor(key), expected));
            }

            public void Approximate(string key, float expected, int data, float delta = 0.01f)
            {
                _translator.FromBios(key, data);
                _oscSender.Verify(s => s.Send(OscCodeFor(key), It.IsInRange(expected - delta, expected + delta, Range.Inclusive)));
            }

            private static string OscCodeFor(in string address) => $"/{address}";
        }

        protected class ToBiosVerifier
        {
            private readonly IOscTranslator _translator;
            private readonly Mock<IBiosSendClient> _biosSender;

            public ToBiosVerifier(IOscTranslator translator, Mock<IBiosSendClient> biosSender) =>
                (_translator, _biosSender) = (translator, biosSender);

            public void Exactly(string oscAddress, int expected, float data, string? biosAddress = null)
            {
                Exactly(oscAddress, expected.ToString(), data, biosAddress);
            }

            public void Exactly(string oscAddress, string expected, float data, string? biosAddress = null)
            {
                _translator.FromOsc(IpAddress, oscAddress, data);
                _biosSender.Verify(s => s.Send(biosAddress ?? oscAddress, expected));
            }
        }
    }
}