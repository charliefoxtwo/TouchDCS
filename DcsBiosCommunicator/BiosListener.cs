using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BiosConfiguration;
using Core.Logging;
using DcsBiosCommunicator.DataParsers;

namespace DcsBiosCommunicator
{
    public class BiosListener : IDisposable
    {
        private readonly BiosStateMachine _parser = new();
        private readonly IUdpReceiveClient _client;
        private readonly Dictionary<int, IntegerHandler> _integerActions = new();
        private readonly Dictionary<int, StringParser> _stringActions = new();
        private Task? _delegateThread;

        private readonly CancellationTokenSource _cts = new();

        private readonly IBiosTranslator _biosTranslator;
        private readonly ILogger _log;

        public BiosListener(in IUdpReceiveClient client, in IBiosTranslator biosTranslator, in ILogger logger)
        {
            _client = client;
            _parser.OnDataWrite += OnBiosDataReceived;
            _biosTranslator = biosTranslator;
            _log = logger;
        }

        public void RegisterConfiguration(AircraftBiosConfiguration configuration)
        {
            foreach (var control in configuration.Values.SelectMany(c => c.Values))
            {
                RegisterControl(control);
            }
        }

        private void RegisterControl(BiosControl control)
        {
            foreach (var output in control.Outputs)
            {
                switch (output)
                {
                    case OutputInteger io:
                        RegisterIntegerControl(control, io);
                        break;
                    case OutputString so:
                        RegisterStringControl(control, so);
                        break;
                    default:
                        throw new ArgumentException($"invalid output type: {output.GetType()}");
                }
            }
        }

        private void RegisterIntegerControl(BiosControl control, OutputInteger output)
        {
            var newParser = new IntegerParser(output.Mask, output.ShiftBy, control.Identifier);
            if (_integerActions.TryGetValue(output.Address, out var handler))
            {
                handler.MaskShifts.Add(newParser);
            }
            else
            {
                RegisterIntegerAddress(new IntegerHandler(output.Address, new[] { newParser }));
            }
        }

        private void RegisterStringControl(BiosControl control, OutputString output)
        {
            RegisterStringAddress(new StringParser(output.Address, output.MaxLength, control.Identifier));
        }

        private void RegisterIntegerAddress(IntegerHandler handler)
        {
            _integerActions[handler.Address] = handler;
        }

        private void RegisterStringAddress(StringParser parser)
        {
            for (var i = 0; i < parser.Length; i++)
            {
                _stringActions.Add(parser.Address + i, parser);
            }
        }

        private void OnBiosDataReceived(int address, int data)
        {
            if (_integerActions.TryGetValue(address, out var handler))
            {
                _log.Trace($"{address:x4} -> got int data -> {data:x4}");
                foreach (var mask in handler.MaskShifts)
                {
                    var success = mask.TryGetValue(address, data, out var result);
                    if (!success || !mask.NeedsSync(result)) continue;
                    mask.LastValue = result;

                    _biosTranslator.FromBios(mask.BiosCode, result);
                }
            }
            // some controls are registered to both strings and integers, because life is fun like that.
            if (_stringActions.TryGetValue(address, out var parser))
            {
                _log.Trace($"{address:x4} -> got string data -> {data:x4}");
                var success = parser.TryGetValue(address, data, out var result);
                if (!success || !parser.NeedsSync(result)) return;
                parser.LastValue = result;

                _biosTranslator.FromBios(parser.BiosCode, result);
            }
        }

        public void Start()
        {
            _log.Debug("Starting DCS-BIOS listener...");

            if (!_cts.IsCancellationRequested && _delegateThread is null)
            {
                _delegateThread = Listener(_cts.Token);
            }

            _log.Info("DCS-BIOS listener started.");
        }

        public void Stop()
        {
            _log.Debug("Stopping DCS-BIOS listener...");
            _cts.Cancel();
            _delegateThread?.Wait();
            _log.Info("DCS-BIOS listener stopped.");
        }

        private async Task Listener(CancellationToken ctx)
        {
            while (!ctx.IsCancellationRequested)
            {
                var data = await _client.ReceiveAsync();
                _log.Trace($"bios data received!: {data.Buffer.Length}");
                try
                {
                    _parser.ProcessBytes(data.Buffer);
                }
                catch (Exception ex)
                {
                    _log.Fatal(ex.ToString());
                    throw;
                }
            }

            _log.Warn("Stopping DCS-BIOS Listener...");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Stop();
            _delegateThread?.Dispose();
            _cts.Dispose();
        }
    }
}