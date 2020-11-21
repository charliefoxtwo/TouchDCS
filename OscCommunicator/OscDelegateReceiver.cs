using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Logging;
using Rug.Osc;

namespace OscCommunicator
{
    public class OscDelegateReceiver : IDisposable
    {
        private readonly OscReceiver _receiver;
        private Task? _delegateThread;

        private readonly IOscTranslator _oscTranslator;
        private readonly ILogger _log;

        private readonly CancellationTokenSource _cts = new();

        public OscDelegateReceiver(in int receivePort, in IOscTranslator oscTranslator, in ILogger log)
        {
            _receiver = new OscReceiver(receivePort);
            _oscTranslator = oscTranslator;
            _log = log;
        }

        public void Connect()
        {
            _log.Debug("Setting up OSC listener...");
            _receiver.Connect();
            _delegateThread = Task.Run(() => Listener(_cts.Token), _cts.Token);
            _log.Info("OSC listener set up.");
        }

        public void Close()
        {
            _log.Debug("Shutting down OSC listener...");
            _cts.Cancel();
            _delegateThread?.Wait();
            _receiver.Close();
            _log.Info("OSC listener shut down.");
        }

        private void Listener(CancellationToken ctx)
        {
            try
            {
                while (_receiver.State != OscSocketState.Closed && !ctx.IsCancellationRequested)
                {
                    if (_receiver.State != OscSocketState.Connected) break;

                    var packet = _receiver.Receive();

                    var message = (OscMessage) packet;
                    _log.Debug($"osc message received {{{packet}}}");

                    var results = message.ToArray();

                    if (results is null) continue;

                    if (results.Length == 1)
                    {
                        _oscTranslator.FromOsc(packet.Origin.Address.ToString(), message.Address, Convert.ToSingle(results[0]));
                    }
                    else
                    {
                        _oscTranslator.FromOsc(packet.Origin.Address.ToString(), message.Address, results.Select(Convert.ToSingle));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Fatal(ex.ToString());
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Close();
            _receiver.Dispose();
            _delegateThread?.Dispose();
            _cts.Dispose();
        }
    }
}