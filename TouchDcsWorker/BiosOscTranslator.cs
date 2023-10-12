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

        private readonly IReadOnlySet<string> _nonAircraftModules;

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

        private readonly Dictionary<string, HashSet<string>> _aircraftAliases = new();

        private readonly HashSet<string> _badAddresses = new();

        private readonly IBiosSendClient _biosSender;

        public BiosOscTranslator(in List<IOscSendClient> oscSenders, in IBiosSendClient biosSender,
            IEnumerable<AircraftBiosConfiguration> biosConfigs, in HashSet<string> nonAircraftModules,
            in ILogger<BiosOscTranslator> logger)
        {
            _oscSenders = oscSenders.ToDictionary(s => s.DeviceIpAddress, s => s);
            _biosSender = biosSender;
            _nonAircraftModules = nonAircraftModules;
            _log = logger;

            foreach (var aircraftConfig in biosConfigs)
            {
                foreach (var alias in aircraftConfig.Aliases)
                {
                    if (!_aircraftAliases.ContainsKey(alias))
                    {
                        _aircraftAliases[alias] = new HashSet<string>();
                    }
                    _aircraftAliases[alias].Add(aircraftConfig.AircraftName);
                }

                var aircraftBiosInputs = new BiosCodeInfo<BiosInput>();

                foreach (var control in aircraftConfig.Values.SelectMany(category => category.Values))
                {
                    if (!$"/{control.Identifier}".IsValidOscAddress())
                    {
                        _log.LogWarning("Invalid OSC address {{/{Address}}} found for aircraft {{{AircraftName}}}. Skipping...",
                            control.Identifier, aircraftConfig.AircraftName);
                        _badAddresses.Add(control.Identifier);
                        continue;
                    }

                    var controlBiosInputInfo = new BiosInputInfo(control.Inputs);

                    if (!aircraftBiosInputs.TryAdd(control.Identifier, controlBiosInputInfo))
                    {
                        _log.LogWarning("Duplicate key {{{Address}}} found for aircraft {{{AircraftName}}}. Skipping...",
                            control.Identifier, aircraftConfig.AircraftName);
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

            _log.LogDebug("processing OSC data {{{Address} {Data}}}", address, data);

            // FromBios will set the active aircraft.
            // check to see if this is an aircraft input
            if (!(_activeAircraft is not null && _allAircraftBiosInputs.TryGetValue(_activeAircraft, out var inputInfos) && inputInfos.TryGetValue(address, out var inputInfo)))
            {
                // if not, maybe it's a module input?
                if (!_allModuleBiosInputs.TryGetValue(address, out inputInfo))
                {
                    _log.LogWarning("Unable to find matching DCS-BIOS command for {Address} in aircraft {ActiveAircraft}", address, _activeAircraft);
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
                    _log.LogError("unable to convert data {{{Data}}} to to any known type", data);
                    return;
            }

            var inputs = inputInfo.BiosData;

            var setStateInput = inputs.OfType<InputSetState>().FirstOrDefault();
            var fixedStepInput = inputs.OfType<InputFixedStep>().FirstOrDefault();
            var variableStepInput = inputs.OfType<InputVariableStep>().FirstOrDefault();
            var actionInput = inputs.OfType<InputAction>().FirstOrDefault();
            var setStringInput = inputs.OfType<InputSetString>().FirstOrDefault();
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
                        _log.LogError("Unrecognized string value {StringData} for {Address}", stringData, address);
                    }
                }

                var amount = floatData ?? (stringPositive ? 1 : -1) * variableStepInput.SuggestedStep;
                var stringifiedAmount = $"{(amount < 0 ? string.Empty : "+")}{amount}";
                _biosSender.Send(address, stringifiedAmount);
            }
            else if ((actionInput is not null || setStringInput is not null) && stringData is not null)
            {
                _biosSender.Send(address, stringData);
            }
            else
            {
                _log.LogError("no supported input found for control {Address}", address);
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
                    _aircraftAliases.TryGetValue(aircraftName, out var alias) ? alias.Except(_nonAircraftModules).First() : aircraftName;

                return;
            }

            var oscAddress = $"/{biosCode}";

            foreach (var sendClient in _oscSenders.Values)
            {
                if (_badAddresses.Contains(biosCode)) continue;

                var success = sendClient.Send(oscAddress, data);

                if (!success && !oscAddress.IsValidOscAddress())
                {
                    // stop trying to send - it's just not going to work.
                    _badAddresses.Add(biosCode);
                    break;
                }
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
