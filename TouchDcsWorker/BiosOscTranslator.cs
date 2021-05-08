using System.Collections.Generic;
using System.Linq;
using DcsBios.Communicator;
using DcsBios.Communicator.Configuration;
using Microsoft.Extensions.Logging;
using OscCommunicator;

namespace TouchDcsWorker
{
    public class BiosOscTranslator : IBiosTranslator, IOscTranslator
    {
        private const string NoneAircraft = "NONE";

        // dcs will be providing us with the aircraft we are flying
        // we'll use the aircraft name to pull the bios configuration
        // we need this because touchosc is going to tell us "send to this address"
        // but multiple aircraft may have the same string address for controls

        private string? _activeAircraft;

        private readonly ILogger<BiosOscTranslator> _log;

        /// <summary>
        /// IP Address -> client
        /// </summary>
        private readonly Dictionary<string, IOscSendClient> _oscSenders;

        // this _can_ be a thing (think NS430)
        /// <summary>
        /// bios code -> bios input info
        /// </summary>
        private readonly BiosCodeInfo<BiosInput> _allModuleBiosInputs = new();

        /// <summary>
        /// aircraft name -> (bios code -> bios input info)
        /// </summary>
        private readonly Dictionary<string, BiosCodeInfo<BiosInput>> _allAircraftBiosInputs = new();

        private readonly Dictionary<string, string> _aircraftAliases = new();

        private readonly IBiosSendClient _biosSender;

        public BiosOscTranslator(in List<IOscSendClient> oscSenders, in IBiosSendClient biosSender,
            IEnumerable<AircraftBiosConfiguration> biosConfigs, in HashSet<string> nonAircraftModules,
            in Dictionary<string, HashSet<string>>? aliases, in ILogger<BiosOscTranslator> logger)
        {
            _oscSenders = oscSenders.ToDictionary(s => s.DeviceIpAddress, s => s);
            _biosSender = biosSender;
            if (aliases != null)
            {
                foreach (var (baseAircraft, aircraftAliases) in aliases)
                {
                    foreach (var alias in aircraftAliases)
                    {
                        _aircraftAliases[alias] = baseAircraft;
                    }
                }
            }
            _log = logger;

            foreach (var aircraftConfig in biosConfigs)
            {
                var aircraftBiosInputs = new BiosCodeInfo<BiosInput>();

                foreach (var control in aircraftConfig.Values.SelectMany(category => category.Values))
                {
                    var controlBiosInputInfo = new BiosInputInfo(control.Inputs);

                    if (!aircraftBiosInputs.TryAdd(control.Identifier, controlBiosInputInfo))
                    {
                        _log.LogWarning($"Duplicate key {{{control.Identifier}}} found for aircraft {{{aircraftConfig.AircraftName}}}. Skipping...");
                    }
                }

                if (nonAircraftModules.Contains(aircraftConfig.AircraftName))
                {
                    foreach (var (biosCode, biosInputInfo) in aircraftBiosInputs)
                    {
                        _allModuleBiosInputs.Add(biosCode, biosInputInfo);
                    }
                }
                else
                {
                    _allAircraftBiosInputs.Add(aircraftConfig.AircraftName, aircraftBiosInputs);
                }
            }
        }

        #region From Osc

        public void FromOsc<T>(string ipAddress, string address, T data)
        {
            // remove the leading slash from the address
            address = address.TrimStart('/');

            _log.LogDebug($"processing OSC data {{{address} {data}}}");

            // FromBios will set the active aircraft.
            // check to see if this is an aircraft input
            if (!(_activeAircraft is not null && _allAircraftBiosInputs.TryGetValue(_activeAircraft, out var inputInfos) && inputInfos.TryGetValue(address, out var inputInfo)))
            {
                // if not, maybe it's a module input?
                if (!_allModuleBiosInputs.TryGetValue(address, out inputInfo))
                {
                    _log.LogWarning($"Unable to find matching DCS-BIOS command for {address} in aircraft {_activeAircraft}");
                    return;
                }
            }

            float? floatData = null;
            string? stringData = null;

            switch (data)
            {
                case float d:
                    floatData = d;
                    break;
                case int i:
                    floatData = i;
                    break;
                case string s:
                    stringData = s;
                    break;
                default:
                    _log.LogError($"unable to convert data {{{data}}} to to any known type.");
                    return;
            }

            var inputs = inputInfo.BiosData;

            var setStateInput = inputs.OfType<InputSetState>().FirstOrDefault();
            var fixedStepInput = inputs.OfType<InputFixedStep>().FirstOrDefault();
            var variableStepInput = inputs.OfType<InputVariableStep>().FirstOrDefault();
            if (setStateInput != null && floatData.HasValue)
            {
                _biosSender.Send(address, floatData.Value.ToString());
            }
            else if (fixedStepInput != null)
            {
                if (floatData == 0) return; // button release, don't do anything

                _biosSender.Send(address, stringData ?? (floatData < 0 ? InputFixedStep.Decrement : InputFixedStep.Increment));
            }
            else if (variableStepInput != null && !(floatData is null && stringData is null))
            {
                if (floatData == 0) return; // button release, don't do anything
                var stringPositive = false;
                if (stringData != null)
                {
                    if (stringData == InputFixedStep.Increment) stringPositive = true;
                    else if (stringData != InputFixedStep.Decrement)
                    {
                        _log.LogError($"Unrecognized string value {stringData} for {address}");
                    }
                }

                var amount = floatData ?? (stringPositive ? 1 : -1) * variableStepInput.SuggestedStep;
                var stringifiedAmount = $"{(amount < 0 ? string.Empty : "+")}{amount}";
                _biosSender.Send(address, stringifiedAmount);
            }
            else
            {
                _log.LogError($"input type {{set_state}} not found for control {address}");
            }
        }

        #endregion

        #region From Bios

        public void FromBios<T>(string biosCode, T data)
        {
            if (data is null) return;

            // if we don't recognize this, just gtfo
            if (biosCode == BiosListener.AircraftNameBiosCode && data is string aircraftName && aircraftName != _activeAircraft)
            {
                // if we are sending "NONE", then we don't currently have an aircraft.
                _activeAircraft = aircraftName == NoneAircraft ? null :
                    _aircraftAliases.TryGetValue(aircraftName, out var alias) ? alias : aircraftName;

                return;
            }

            foreach (var sendClient in _oscSenders.Values)
            {
                sendClient.Send($"/{biosCode}", data);
            }
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