using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configuration;
using DcsBios.Communicator;
using DcsBios.Communicator.Configuration;
using Microsoft.Extensions.Logging;
using OscCommunicator;

namespace TouchDcsWorker
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Worker
    {
        public static async Task Run(ILoggerFactory loggerFactory)
        {
            var appConfig = ApplicationConfiguration.Get();
            var log = loggerFactory.CreateLogger<Worker>();

            if (appConfig is null)
            {
                log.LogWarning("No configuration detected; creating default config.json");
                ApplicationConfiguration.CreateNewConfiguration();
                log.LogInformation("Edit osc.configLocations and osc.devices in accordance with your setup, and then restart TouchDCS");
                Console.ReadKey(true);
                return;
            }

            var oscSenders = SetUpOscSenders(appConfig, loggerFactory);
            var biosUdpClient = SetUpBiosUdpClient(appConfig, loggerFactory.CreateLogger<BiosUdpClient>());

            var aircraftBiosConfigs = await AircraftBiosConfiguration.AllConfigurations(appConfig.DcsBios.ConfigLocations.ToArray());

            var translator = new BiosOscTranslator(oscSenders.ToList(), biosUdpClient, aircraftBiosConfigs,
                appConfig.CommonModules ?? new HashSet<string>(), appConfig.Aliases, loggerFactory.CreateLogger<BiosOscTranslator>());

            var biosListener = new BiosListener(biosUdpClient, translator, loggerFactory.CreateLogger<BiosListener>());
            foreach (var config in aircraftBiosConfigs)
            {
                biosListener.RegisterConfiguration(config);
            }
            biosListener.Start();

            var _ = SetUpOscReceivers(appConfig, translator, loggerFactory);

            log.LogInformation("Ready!");

            await Task.Delay(-1);
        }

        private static List<OscDelegateReceiver> SetUpOscReceivers(ApplicationConfiguration appConfig,
            IOscTranslator translator, ILoggerFactory loggerFactory)
        {
            var receivers = new List<OscDelegateReceiver>();
            foreach (var oscReceiver in appConfig.Osc.Devices.Select(device =>
                new OscDelegateReceiver(device.ReceivePort, translator,
                    loggerFactory.CreateLogger<OscDelegateReceiver>())))
            {
                oscReceiver.Connect();
                receivers.Add(oscReceiver);
            }

            return receivers;
        }

        private static IEnumerable<IOscSendClient> SetUpOscSenders(ApplicationConfiguration appConfig, ILoggerFactory loggerFactory)
        {
            foreach (var sendClient in appConfig.Osc.Devices.Select(device =>
                new OscSendClient(device.Ip, device.SendPort,
                    loggerFactory.CreateLogger<OscSendClient>())))
            {
                sendClient.Connect();
                yield return sendClient;
            }
        }

        private static BiosUdpClient SetUpBiosUdpClient(ApplicationConfiguration appConfig, ILogger<BiosUdpClient> log)
        {
            if (appConfig.DcsBios.Export is null)
            {
                log.LogError("No bios export ip specified. Exiting...");
                throw new ArgumentException("Application configuration must have dcs bios export ip",
                    nameof(appConfig));
            }

            var client = new BiosUdpClient(appConfig.DcsBios.Export.Ip, appConfig.DcsBios.Export.SendPort,
                appConfig.DcsBios.Export.ReceivePort, log);
            client.OpenConnection();
            return client;
        }
    }
}