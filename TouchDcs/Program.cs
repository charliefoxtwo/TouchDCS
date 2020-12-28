using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BiosConfiguration;
using Configuration;
using Core;
using Core.Logging;
using DcsBiosCommunicator;
using OscCommunicator;
using OscConfiguration;

namespace TouchDcs
{
    // TODO: Test suite for all gauge types and config types

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var appConfig = ApplicationConfiguration.Get();
            var log = new Acacia(nameof(TouchDcs), appConfig?.LogLevel ?? LogLevel.Info);
            if (appConfig is null)
            {
                log.Warn("No configuration detected; creating default config.json");
                ApplicationConfiguration.CreateNewConfiguration();
                log.Info("Edit osc.configLocations and osc.devices in accordance with your setup, and then restart TouchDCS");
                Console.ReadKey(true);
                return;
            }

            var oscSenders = SetUpOscSenders(appConfig);
            var biosUdpClient = SetUpBiosUdpClient(appConfig);

            var aircraftBiosConfigs = await GetAircraftBiosConfigurations(appConfig);
            var aircraftOscConfigs = GetAircraftOscConfigurations(appConfig);


            var translator = new BiosOscTranslator(oscSenders.ToList(), biosUdpClient, aircraftBiosConfigs,
                aircraftOscConfigs, appConfig.CommonModules.ToHashSet(),
                new Acacia(nameof(BiosOscTranslator), appConfig.LogLevel));

            var biosListener = new BiosListener(biosUdpClient, translator, new Acacia(nameof(BiosListener), appConfig.LogLevel));
            foreach (var config in aircraftBiosConfigs)
            {
                biosListener.RegisterConfiguration(config);
            }
            biosListener.Start();

            var _ = SetUpOscReceivers(appConfig, translator);

            log.Info("Ready!");

            await Task.Delay(-1);
        }

        private static async Task<AircraftBiosConfiguration[]> GetAircraftBiosConfigurations(
            ApplicationConfiguration appConfig)
        {
            var configTasks = appConfig.DcsBios.ConfigLocations
                .SelectMany(GetAllJsonFilesBelowDirectory)
                .Select(AircraftBiosConfiguration.BuildFromConfiguration);

            return await Task.WhenAll(configTasks);
        }

        private static IEnumerable<AircraftOscConfiguration> GetAircraftOscConfigurations(
            ApplicationConfiguration appConfig)
        {
            return appConfig.Osc.ConfigLocations
                .SelectMany(GetAllJsonFilesBelowDirectory)
                .Select(AircraftOscConfiguration.BuildFromFile).ToList();
        }

        private static IEnumerable<FileInfo> GetAllJsonFilesBelowDirectory(string directory)
        {
            var finalPath = PathHelpers.FullOrRelativePath(Environment.ExpandEnvironmentVariables(directory));
            return Directory.Exists(finalPath)
                ? new DirectoryInfo(finalPath).EnumerateFiles("*.json", SearchOption.AllDirectories)
                : Enumerable.Empty<FileInfo>();
        }

        private static List<OscDelegateReceiver> SetUpOscReceivers(ApplicationConfiguration appConfig,
            IOscTranslator translator)
        {
            var receivers = new List<OscDelegateReceiver>();
            foreach (var oscReceiver in appConfig.Osc.Devices.Select(device =>
                new OscDelegateReceiver(device.ReceivePort, translator,
                    new Acacia(nameof(OscDelegateReceiver), appConfig.LogLevel))))
            {
                oscReceiver.Connect();
                receivers.Add(oscReceiver);
            }

            return receivers;
        }

        private static IEnumerable<IOscSendClient> SetUpOscSenders(ApplicationConfiguration appConfig)
        {
            foreach (var sendClient in appConfig.Osc.Devices.Select(device =>
                new OscSendClient(device.Ip, device.SendPort,
                    new Acacia(nameof(OscSendClient), appConfig.LogLevel))))
            {
                sendClient.Connect();
                yield return sendClient;
            }
        }

        private static BiosUdpClient SetUpBiosUdpClient(ApplicationConfiguration appConfig)
        {
            var client = new BiosUdpClient(appConfig.DcsBios.Export.Ip, appConfig.DcsBios.Export.SendPort,
                appConfig.DcsBios.Export.ReceivePort, new Acacia(nameof(BiosUdpClient), appConfig.LogLevel));
            client.OpenConnection();
            return client;
        }
    }
}