using System;
using System.Collections.Generic;
using System.Linq;
using BiosConfiguration;
using Core;
using Core.Logging;
using DcsBiosCommunicator;
using OscCommunicator;
using OscConfiguration;

namespace TouchDcs
{
    public class BiosOscTranslator : IBiosTranslator, IOscTranslator
    {
        // we'll keep track of our osc configurations here
        private readonly Dictionary<SyncAddress, AircraftOscConfiguration> _syncAddressToOscConfiguration =
            new();

        // we'll keep track of our bios configurations here

        // dcs will be providing us with the aircraft we are flying
        // touchosc will be providing us with the syncAddress we are using
        // we'll use the syncAddress to pull the osc configuration
        // and we'll use the aircraft name to pull the bios configuration
        // we need this because touchosc is going to tell us "send to this address"
        // but multiple aircraft may have the same string address for controls

        private string? _activeAircraft;

        private HashSet<string> _guessedAircraft = new();

        private readonly ILogger _log;

        /// <summary>
        /// IP Address -> client
        /// </summary>
        private readonly Dictionary<string, ISendClient> _oscSenders;

        /// <summary>
        /// device ip -> loaded configuration
        /// </summary>
        private readonly Dictionary<string, AircraftOscConfiguration> _activeOscConfigurations = new ();

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

        private readonly ISendClient _biosSender;

        public BiosOscTranslator(in List<ISendClient> oscSenders, in ISendClient biosSender, IEnumerable<AircraftBiosConfiguration> biosConfigs,
            IEnumerable<AircraftOscConfiguration> oscConfigs, in HashSet<string> nonAircraftModules, in ILogger logger)
        {
            _oscSenders = oscSenders.ToDictionary(s => s.DeviceIpAddress, s => s);
            _biosSender = biosSender;
            _log = logger;

            // bios code -> (osc code -> osc control)
            var biosCodeToOscControls = new Dictionary<string, SyncAddressOscControls>();

            foreach (var oscConfig in oscConfigs)
            {
                var configSyncAddress = new SyncAddress(oscConfig.SyncAddress);
                if (!_syncAddressToOscConfiguration.TryAdd(configSyncAddress, oscConfig))
                {
                    _log.Error($"Duplicate sync address detected: {{{oscConfig.SyncAddress}}}");
                    continue;
                }
                foreach (var (oscCode, oscControl) in oscConfig.Properties)
                {
                    var key = oscControl.BiosProperty ?? oscCode!;
                    if (!biosCodeToOscControls.ContainsKey(key))
                    {
                        biosCodeToOscControls[key] = new SyncAddressOscControls();
                    }

                    if (!biosCodeToOscControls[key].ContainsKey(configSyncAddress))
                    {
                        biosCodeToOscControls[key][configSyncAddress] = new IndexedOscDictionary();
                    }

                    biosCodeToOscControls[key][configSyncAddress].Add(oscCode, oscControl);
                }
            }
            // var y = oscConfigs.SelectMany(o => o.Values).GroupBy(c => c.BiosProperty).ToDictionary(g => g.Key, g => g.ToList());


            // aircraft name -> (bios code -> biosOutputInfo)

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

                    // find any osc controls with matching names
                    var oscControls = biosCodeToOscControls.TryGetValue(control.Identifier, out var oscOverride)
                        ? oscOverride
                        : null;
                    var controlBiosOutputInfo = new BiosOutputInfo(control.Outputs, oscControls);
                    var controlBiosInputInfo = new BiosInputInfo(control.Inputs, oscControls);

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
            if (_syncAddressToOscConfiguration.TryGetValue(new SyncAddress(address), out var configuration))
            {
                // this was a sync address, so set the active configuration if necessary and get out
                if (Convert.ToInt32(data) == 1)
                {
                    if (_activeOscConfigurations.ContainsKey(ipAddress) &&
                        _activeOscConfigurations[ipAddress].SyncAddress == configuration.SyncAddress) return;

                    _activeOscConfigurations[ipAddress] = configuration;
                    _log.Info($"Configuration loaded -> {{{configuration.SyncAddress}}}");
                }

                return;
            }

            // if the IP doesn't have an active configuration, the sync address hasn't been called yet.
            if (!_activeOscConfigurations.TryGetValue(ipAddress, out var activeConfiguration))
            {
                _log.Warn($"No profile loaded for {ipAddress}, discarding message.");
                return;
            }

            if (data is not float floatData)
            {
                _log.Error($"unable to convert data {{{data}}} to float.");
                return;
            }

            _log.Debug($"processing OSC data {{{address} {floatData}}}");

            var splitAddress = address.Split('/');
            if (activeConfiguration.Properties.TryGetValue(address, out var oscControl) ||
                activeConfiguration.Properties.TryGetValue(splitAddress[0], out oscControl))
            {
                string newAddress;
                if (oscControl.ControlType == ControlType.MultiToggleExclusive)
                {
                    if (floatData == 0) return;

                    if (splitAddress.Length < 3 || oscControl.Orientation == Orientation.Unknown ||
                        !int.TryParse(splitAddress[^1], out var row) ||
                        !int.TryParse(splitAddress[^2], out var column))
                    {
                        _log.Error($"Attepted to handle multitoggle exclusive {{{address}}}, but unable to parse data");
                        return;
                    }

                    floatData = oscControl.Orientation == Orientation.Horizontal ? column : row;
                    floatData -= 1;
                    // here we're removing the /#/# from the end
                    newAddress = address.Substring(0, address.Length - 4);
                }
                else
                {
                    newAddress = oscControl.BiosProperty ?? address;
                }

                // the aircraft must not have been set yet, so let's just hang tight. Nothing to do here.
                if (_activeAircraft is null || !_allAircraftBiosInputs.TryGetValue(_activeAircraft, out var inputInfos)) return;
                // if we don't recognize the bios code, leave.
                if (!inputInfos.TryGetValue(newAddress, out var biosInputInfo))
                {
                    _log.Warn($"No valid DCS-BIOS found for {newAddress}.");
                    return;
                }
                var inputs = biosInputInfo.BiosData;

                InputVariableStep? variableStepInput = null;
                InputFixedStep? fixedStepInput = null;
                InputSetState? setStateInput = null;

                foreach (var input in inputs)
                {
                    switch (input)
                    {
                        case InputVariableStep vs:
                            variableStepInput = vs;
                            break;
                        case InputFixedStep fs:
                            fixedStepInput = fs;
                            break;
                        case InputSetState ss:
                            setStateInput = ss;
                            break;
                    }
                }

                int res;
                string resStr;

                if (setStateInput != null && !oscControl.IgnoreSetState)
                {
                    if (oscControl.InvertOrientation) floatData = setStateInput.MaxValue - floatData;
                    res = oscControl.ControlType == ControlType.MultiToggleExclusive ? (int) floatData : UnclampDataForBios(floatData, setStateInput.MaxValue);
                    resStr = res.ToString();
                }
                // it seems a variable step often has the set state option
                else if (variableStepInput != null)
                {
                    res = oscControl.FixedStepOverride != 0
                        ? oscControl.FixedStepOverride
                        : variableStepInput.SuggestedStep;
                    resStr = Math.Sign(res) < 0 ? res.ToString() : $"+{res}";
                }
                else if (fixedStepInput != null)
                {
                    if (floatData == 0) return; // button release, don't do anything

                    res = oscControl.FixedStepOverride != 0
                        ? oscControl.FixedStepOverride
                        : (int) floatData;

                    resStr = res < 0 ? InputFixedStep.Decrement : InputFixedStep.Increment;
                }
                else
                {
                    _log.Warn($"No valid DCS-BIOS found for {newAddress}.");
                    return;
                }

                _biosSender.Send(newAddress, resStr).Wait();
            }
            else
            {
                // our job is a lot easier. Just send the data to dcs.

                // FromBios will set the active aircraft.
                // check to see if this is an aircraft input
                BiosInfo<BiosInput>? inputInfo;
                if (!(_activeAircraft is not null && _allAircraftBiosInputs.TryGetValue(_activeAircraft, out var inputInfos) && inputInfos.TryGetValue(address, out inputInfo)))
                {
                    // if not, maybe it's a module input?
                    if (!_allModuleBiosInputs.TryGetValue(address, out inputInfo))
                    {
                        _log.Warn($"Unable to find matching DCS-BIOS command for {address}");
                        return;
                    }
                }

                var inputs = inputInfo.BiosData;

                var setStateInput = inputs.OfType<InputSetState>().FirstOrDefault();
                var fixedStepInput = inputs.OfType<InputFixedStep>().FirstOrDefault();
                if (setStateInput != null)
                {
                    _biosSender.Send(address,
                        setStateInput.MaxValue > 1 ? UnclampDataForBios(floatData, setStateInput.MaxValue) : floatData).Wait();
                }
                else if (fixedStepInput != null)
                {
                    if (floatData == 0) return; // button release, don't do anything
                    _biosSender.Send(address, floatData < 0 ? InputFixedStep.Decrement : InputFixedStep.Increment);
                }
                else
                {
                    _log.Error($"input type {{set_state}} not found for control {address}");
                }
            }
        }

        private static int UnclampDataForBios(float data, int max)
        {
            return Math.Max(0, Math.Min(max, (int) Math.Round(data * max)));
        }

        #endregion

        #region From Bios

        public void FromBios<T>(string biosCode, T data)
        {
            BiosInfo<BiosOutput> outputs;

            // if we don't recognize this, just gtfo
            if (_aircraftForBiosCommand.TryGetValue(biosCode, out var aircrafts))
            {
                if (_activeAircraft is null)
                {
                    if (!_guessedAircraft.Any())
                    {
                        _guessedAircraft = aircrafts;
                        return;
                    }

                    _guessedAircraft.IntersectWith(aircrafts);

                    if (_guessedAircraft.Count != 1) return;
                    _activeAircraft = _guessedAircraft.First();
                    _log.Info($"New aircraft detected -> {{{_activeAircraft}}}");
                }

                if (!_allAircraftBiosOutputs.TryGetValue(_activeAircraft, out var biosOutputInfos))
                {
                    _log.Error($"Aircraft {_activeAircraft} not recognized.");
                    _activeAircraft = null;
                    return;
                }

                if (!biosOutputInfos.TryGetValue(biosCode, out var aircraftOutputs))
                {
                    _log.Warn($"Aircraft {_activeAircraft} does not contain bios code {biosCode}.");
                    _log.Info("Detecting new aircraft...");
                    _activeAircraft = null;
                    return;
                }

                outputs = aircraftOutputs;
            }
            else
            {
                // it could be a module - we should still process this accordingly
                if (!_allModuleBiosOutputs.TryGetValue(biosCode, out var moduleOutputs))
                {
                    _log.Warn($"DCS-BIOS code {biosCode} does not match any modules.");
                    return;
                }

                outputs = moduleOutputs;
            }


            foreach (var sendClient in _oscSenders.Values)
            {
                // if the client doesn't have an osc config loaded then just skip it... nothing to send anyway.
                if (!_activeOscConfigurations.TryGetValue(sendClient.DeviceIpAddress, out var configuration)) continue;

                IEnumerable<OscMessage> messages;

                switch (data)
                {
                    case int i:
                        messages = GetValuesToSendFromBiosToOsc(biosCode, i, outputs, new SyncAddress(configuration.SyncAddress));
                        break;
                    case string s:
                        messages = GetValuesToSendFromBiosToOsc(biosCode, s, outputs, new SyncAddress(configuration.SyncAddress));
                        break;
                    default:
                        return;
                }

                foreach (var (address, sendData) in messages.Append(GetOscCommand($"{configuration.SyncAddress}_STATUS", 1)))
                {
                    sendClient.Send(address, sendData).Wait();
                }
            }
        }

        private static IEnumerable<OscMessage> GetValuesToSendFromBiosToOsc(string biosCode, string data, BiosInfo<BiosOutput> outputs, SyncAddress syncAddress)
        {
            var (_, oscControls) = outputs;

            IndexedOscDictionary? controls = null;
            if (oscControls?.TryGetValue(syncAddress, out controls) != true || controls is null)            {
                yield return GetOscCommand(biosCode, data);
                yield break;
            }

            foreach (var (oscAddress, oscControl) in controls)
            {
                if (oscControl.ReMap is not null && oscControl.ReMap.TryGetValue(data, out var newData))
                {
                    data = newData!;
                }

                yield return GetOscCommand(oscAddress, data);
            }
        }

        private static float ClampDataForOsc(int data, int max)
        {
            return Math.Max(0, Math.Min(1, (float) data / max));
        }

        private static OscMessage GetOscCommand(string oscAddressNoSlash, object data)
        {
            return new($"/{oscAddressNoSlash}", data);
        }

        private static OscMessage GetMultiToggleOscCommand(in OscControl control, in string oscAddressNoSlash, int data, int maxValue)
        {
            if (control.InvertOrientation)
            {
                data = maxValue - data;
            }

            data += 1;

            var str = control.Orientation switch
            {
                Orientation.Horizontal => $"/{oscAddressNoSlash}/{data}/1",
                Orientation.Vertical => $"/{oscAddressNoSlash}/1/{data}",
                Orientation.Unknown => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            return new OscMessage(str, 1);
        }

        private static IEnumerable<OscMessage> GetValuesToSendFromBiosToOsc(string biosCode, int data, BiosInfo<BiosOutput> outputs, SyncAddress syncAddress)
        {
            var (biosOutputs, oscControls) = outputs;
            var biosOutput = biosOutputs.OfType<OutputInteger>().First();

            IndexedOscDictionary? controls = null;
            if (oscControls?.TryGetValue(syncAddress, out controls) != true || controls is null)
            {
                // no overrides, send the standard address
                yield return GetOscCommand(biosCode, biosOutput.MaxValue > 1 ? ClampDataForOsc(data, biosOutput.MaxValue) : data);
                yield break;
            }

            // this is either a selector or rotary.
            // for buttons, we could highlight the "up" button if data > 0, "down" button if less than 0
            // same for LEDs
            // data is going to be a set value, not gt/lt. we can't move buttons, nor encoders

            // for toggles, we'll try to set the toggle value appropriately

            // for faders/rotaries, we just need to clamp the value?
            foreach (var (oscAddress, oscControl) in controls)
            {
                // in this case we can't really map a value to the control, so leave it?
                if (oscControl.ControlType == ControlType.Button && biosOutput.MaxValue > 1) continue;

                if (oscControl.ReMap is not null)
                {
                    if (!oscControl.ReMap.TryGetValue(data.ToString(), out var remapped)) continue;
                    yield return GetOscCommand(oscAddress, remapped);
                }
                else if (oscControl.Transform is not null)
                {
                    var newValue = (double) data;
                    newValue *= oscControl.Transform.Multiply ?? 1;
                    newValue /= oscControl.Transform.Divide ?? 1;
                    newValue += oscControl.Transform.Add ?? 0;
                    newValue -= oscControl.Transform.Subtract ?? 0;
                    var result = Math.Round(newValue, oscControl.Transform.ForceDecimalPlaces);
                    yield return GetOscCommand(oscAddress,
                        result.ToString($"F{oscControl.Transform.ForceDecimalPlaces}"));
                }
                else
                {
                    // TODO: complete these types?
                    yield return oscControl.ControlType switch
                    {
                        // ControlType.Unknown => throw new NotImplementedException(),
                        // ControlType.MultiToggle => throw new NotImplementedException(),
                        ControlType.MultiToggleExclusive => GetMultiToggleOscCommand(oscControl, oscAddress, data,
                            biosOutput.MaxValue),
                        // ControlType.Button => throw new NotImplementedException(),
                        // ControlType.Toggle => throw new NotImplementedException(),
                        _ => GetOscCommand(oscAddress,
                            biosOutput.MaxValue > 1 ? ClampDataForOsc(data, biosOutput.MaxValue) : data),
                    };
                }
            }
        }

        #endregion
    }

    public sealed record BiosOutputInfo
        (List<BiosOutput> BiosOutputs, SyncAddressOscControls? OscControls) : BiosInfo<BiosOutput>(BiosOutputs,
            OscControls);

    public sealed record BiosInputInfo
        (List<BiosInput> BiosInputs, SyncAddressOscControls? OscControls) : BiosInfo<BiosInput>(BiosInputs, OscControls);

    public record BiosInfo<T>(List<T> BiosData, SyncAddressOscControls? OscControls);

    /// <summary>
    /// A dictionary holding a set of OSC Controls indexed by their name.
    /// </summary>
    public class IndexedOscDictionary : Dictionary<string, OscControl> { }

    /// <summary>
    /// sync address -> indexed osc dictionary
    /// This is necessary as osc overrides should *all* be behind a sync address.
    /// </summary>
    public class SyncAddressOscControls : Dictionary<SyncAddress, IndexedOscDictionary> { }

    /// <summary>
    /// bios code -> Bios*Info
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BiosCodeInfo<T> : Dictionary<string, BiosInfo<T>> { }

    /// <summary>
    /// This type exists purely to make code easier to read and more difficult for developers to make errors when making changes.
    /// </summary>
    public sealed record SyncAddress(string Address);

    public sealed record OscMessage(string Address, object Data);
}