using System;
using System.Collections.Generic;
using System.Linq;
using BiosConfiguration;
using Core.Logging;
using DcsBiosCommunicator;
using OscCommunicator;

namespace TouchDcs
{
    public class BiosOscTranslator : IBiosTranslator, IOscTranslator
    {
        private const string NoneAircraft = "NONE";

        // we'll keep track of our osc configurations here
        // private readonly Dictionary<SyncAddress, AircraftOscConfiguration> _syncAddressToOscConfiguration =
        //     new();

        // we'll keep track of our bios configurations here

        // dcs will be providing us with the aircraft we are flying
        // touchosc will be providing us with the syncAddress we are using
        // we'll use the syncAddress to pull the osc configuration
        // and we'll use the aircraft name to pull the bios configuration
        // we need this because touchosc is going to tell us "send to this address"
        // but multiple aircraft may have the same string address for controls

        private string? _activeAircraft;

        private readonly ILogger _log;

        /// <summary>
        /// IP Address -> client
        /// </summary>
        private readonly Dictionary<string, IOscSendClient> _oscSenders;

        /// <summary>
        /// bios code -> bios output info
        /// </summary>
        private readonly BiosCodeInfo<BiosOutput> _allModuleBiosOutputs = new();

        // this _can_ be a thing (think NS430)
        /// <summary>
        /// bios code -> bios input info
        /// </summary>
        private readonly BiosCodeInfo<BiosInput> _allModuleBiosInputs = new();

        /// <summary>
        /// aircraft name -> (bios code -> bios output info)
        /// </summary>
        private readonly Dictionary<string, BiosCodeInfo<BiosOutput>> _allAircraftBiosOutputs = new();

        /// <summary>
        /// aircraft name -> (bios code -> bios input info)
        /// </summary>
        private readonly Dictionary<string, BiosCodeInfo<BiosInput>> _allAircraftBiosInputs = new();

        /// <summary>
        /// bios code -> aircraft name
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _aircraftForBiosCommand = new();

        private readonly IBiosSendClient _biosSender;

        public BiosOscTranslator(in List<IOscSendClient> oscSenders, in IBiosSendClient biosSender, 
            IEnumerable<AircraftBiosConfiguration> biosConfigs, in HashSet<string> nonAircraftModules, in ILogger logger)
        {
            _oscSenders = oscSenders.ToDictionary(s => s.DeviceIpAddress, s => s);
            _biosSender = biosSender;
            _log = logger;

            // bios code -> (osc code -> osc control)
            // var biosCodeToOscControls = new Dictionary<string, SyncAddressOscControls>();

            // foreach (var oscConfig in oscConfigs)
            // {
            //     var configSyncAddress = new SyncAddress(oscConfig.SyncAddress);
            //     if (!_syncAddressToOscConfiguration.TryAdd(configSyncAddress, oscConfig))
            //     {
            //         _log.Error($"Duplicate sync address detected: {{{oscConfig.SyncAddress}}}");
            //         continue;
            //     }
            //     foreach (var (oscCode, oscControl) in oscConfig.Properties)
            //     {
            //         var key = oscControl.BiosProperty ?? oscCode!;
            //         if (!biosCodeToOscControls.ContainsKey(key))
            //         {
            //             biosCodeToOscControls[key] = new SyncAddressOscControls();
            //         }
            //
            //         if (!biosCodeToOscControls[key].ContainsKey(configSyncAddress))
            //         {
            //             biosCodeToOscControls[key][configSyncAddress] = new IndexedOscDictionary();
            //         }
            //
            //         biosCodeToOscControls[key][configSyncAddress].Add(oscCode, oscControl);
            //     }
            // }

            foreach (var aircraftConfig in biosConfigs)
            {
                // biosCode -> biosoutputinfo
                var aircraftBiosOutputs = new BiosCodeInfo<BiosOutput>();
                var aircraftBiosInputs = new BiosCodeInfo<BiosInput>();

                foreach (var control in aircraftConfig.Values.SelectMany(category => category.Values))
                {
                    // record which aircraft this control belongs to
                    if (!nonAircraftModules.Contains(aircraftConfig.AircraftName))
                    {
                        if (!_aircraftForBiosCommand.ContainsKey(control.Identifier))
                        {
                            _aircraftForBiosCommand[control.Identifier] = new HashSet<string>();
                        }

                        _aircraftForBiosCommand[control.Identifier].Add(aircraftConfig.AircraftName);
                    }

                    var controlBiosOutputInfo = new BiosOutputInfo(control.Outputs);
                    var controlBiosInputInfo = new BiosInputInfo(control.Inputs);

                    if (!aircraftBiosOutputs.TryAdd(control.Identifier, controlBiosOutputInfo))
                    {
                        _log.Warn($"Duplicate key {{{control.Identifier}}} found for aircraft {{{aircraftConfig.AircraftName}}}");
                    }
                    else
                    {
                        aircraftBiosInputs.Add(control.Identifier, controlBiosInputInfo);
                    }
                }

                if (nonAircraftModules.Contains(aircraftConfig.AircraftName))
                {
                    foreach (var (biosCode, biosOutputInfo) in aircraftBiosOutputs)
                    {
                        _allModuleBiosOutputs.Add(biosCode, biosOutputInfo);
                    }

                    foreach (var (biosCode, biosInputInfo) in aircraftBiosInputs)
                    {
                        _allModuleBiosInputs.Add(biosCode, biosInputInfo);
                    }
                }
                else
                {
                    _allAircraftBiosOutputs.Add(aircraftConfig.AircraftName, aircraftBiosOutputs);
                    _allAircraftBiosInputs.Add(aircraftConfig.AircraftName, aircraftBiosInputs);
                }
            }
        }

        #region From Osc

        public void FromOsc<T>(string ipAddress, string address, T data)
        {
            // remove the leading slash from the address
            address = address.TrimStart('/');

            _log.Debug($"processing OSC data {{{address} {data}}}");

            // FromBios will set the active aircraft.
            // check to see if this is an aircraft input
            if (!(_activeAircraft is not null && _allAircraftBiosInputs.TryGetValue(_activeAircraft, out var inputInfos) && inputInfos.TryGetValue(address, out var inputInfo)))
            {
                // if not, maybe it's a module input?
                if (!_allModuleBiosInputs.TryGetValue(address, out inputInfo))
                {
                    _log.Warn($"Unable to find matching DCS-BIOS command for {address} in aircraft {_activeAircraft}");
                    return;
                }
            }

            float? floatData = null;
            string? stringData = null;

            // var floatData = 0f;
            if (data is float d)
            {
                floatData = d;
            } else if (data is int i)
            {
                floatData = i;
            } else if (data is string s)
            {
                stringData = s;
            }
            else
            {
                _log.Error($"unable to convert data {{{data}}} to to any known type.");
                return;
            }

            var inputs = inputInfo.BiosData;

            var setStateInput = inputs.OfType<InputSetState>().FirstOrDefault();
            var fixedStepInput = inputs.OfType<InputFixedStep>().FirstOrDefault();
            if (setStateInput != null && floatData.HasValue)
            {
                _biosSender.Send(address, floatData.Value.ToString());
            }
            else if (fixedStepInput != null)
            {
                if (floatData == 0) return; // button release, don't do anything

                _biosSender.Send(address, stringData ?? (floatData < 0 ? InputFixedStep.Decrement : InputFixedStep.Increment));
            }
            else
            {
                _log.Error($"input type {{set_state}} not found for control {address}");
            }
        }

        #endregion

        #region From Bios

        public void FromBios<T>(string biosCode, T data)
        {
            // if we don't recognize this, just gtfo
            if (biosCode == BiosListener.AircraftNameBiosCode && data is string aircraftName && aircraftName != _activeAircraft)
            {
                // if we are sending "NONE", then we don't currently have an aircraft.
                _activeAircraft = aircraftName == NoneAircraft ? null : aircraftName;

                return;
            }

            foreach (var sendClient in _oscSenders.Values)
            {
                OscMessage message;

                switch (data)
                {
                    case int i:
                        message = GetOscCommand(biosCode, i);
                        break;
                    case string s:
                        message = GetOscCommand(biosCode, s);
                        break;
                    default:
                        return;
                }

                sendClient.Send(message.Address, message.Data);
            }
        }

        private static OscMessage GetOscCommand(string oscAddressNoSlash, object data)
        {
            return new($"/{oscAddressNoSlash}", data);
        }

        #endregion
    }

    public sealed record BiosOutputInfo
        (List<BiosOutput> BiosOutputs) : BiosInfo<BiosOutput>(BiosOutputs);

    public sealed record BiosInputInfo
        (List<BiosInput> BiosInputs) : BiosInfo<BiosInput>(BiosInputs);

    public record BiosInfo<T>(List<T> BiosData);

    /// <summary>
    /// bios code -> Bios*Info
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BiosCodeInfo<T> : Dictionary<string, BiosInfo<T>> { }

    public sealed record OscMessage(string Address, object Data);
}