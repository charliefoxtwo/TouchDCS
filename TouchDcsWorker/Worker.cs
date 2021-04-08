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

namespace TouchDcsWorker
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Worker
    {
        public static async Task Run(ILogger log)
        {
            var appConfig = ApplicationConfiguration.Get();

            if (appConfig is null)
            {
                log.Warn("No configuration detected; creating default config.json");
                ApplicationConfiguration.CreateNewConfiguration();
                log.Info("Edit osc.configLocations and osc.devices in accordance with your setup, and then restart TouchDCS");
                Console.ReadKey(true);
                return;
            }

            log.SetLogLevel(appConfig.LogLevel);

            var oscSenders = SetUpOscSenders(appConfig, log);
            var biosUdpClient = SetUpBiosUdpClient(appConfig, log);

            var aircraftBiosConfigs = await GetAircraftBiosConfigurations(appConfig);

            var translator = new BiosOscTranslator(oscSenders.ToList(), biosUdpClient, aircraftBiosConfigs,
                appConfig.CommonModules.ToHashSet(), log);

            var biosListener = new BiosListener(biosUdpClient, translator, log);
            foreach (var config in aircraftBiosConfigs)
            {
                biosListener.RegisterConfiguration(config);
            }
            biosListener.Start();

            var _ = SetUpOscReceivers(appConfig, translator, log);

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

        private static IEnumerable<FileInfo> GetAllJsonFilesBelowDirectory(string directory)
        {
            var finalPath = PathHelpers.FullOrRelativePath(Environment.ExpandEnvironmentVariables(directory));
            return Directory.Exists(finalPath)
                ? new DirectoryInfo(finalPath).EnumerateFiles("*.json", SearchOption.AllDirectories)
                : Enumerable.Empty<FileInfo>();
        }

        private static List<OscDelegateReceiver> SetUpOscReceivers(ApplicationConfiguration appConfig,
            IOscTranslator translator, ILogger log)
        {
            var receivers = new List<OscDelegateReceiver>();
            foreach (var oscReceiver in appConfig.Osc.Devices.Select(device =>
                new OscDelegateReceiver(device.ReceivePort, translator,
                    log)))
            {
                oscReceiver.Connect();
                receivers.Add(oscReceiver);
            }

            return receivers;
        }

        private static IEnumerable<IOscSendClient> SetUpOscSenders(ApplicationConfiguration appConfig, ILogger log)
        {
            foreach (var sendClient in appConfig.Osc.Devices.Select(device =>
                new OscSendClient(device.Ip, device.SendPort,
                    log)))
            {
                sendClient.Connect();
                yield return sendClient;
            }
        }

        private static BiosUdpClient SetUpBiosUdpClient(ApplicationConfiguration appConfig, ILogger log)
        {
            var client = new BiosUdpClient(appConfig.DcsBios.Export.Ip, appConfig.DcsBios.Export.SendPort,
                appConfig.DcsBios.Export.ReceivePort, log);
            client.OpenConnection();
            return client;
        }
    }
}