using System;
using System.Runtime.InteropServices;

namespace Core.Logging
{
    public class Acacia : ILogger
    {
        private readonly string _name;
        private readonly LogLevel _outputLevel;
        private static volatile bool _consolePrepped = false;
        private static object _prepLock = new();

        public Acacia(in string name, in LogLevel outputLevel = LogLevel.Warn)
        {
            _name = name;
            _outputLevel = outputLevel;

            if (!_consolePrepped)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    lock (_prepLock)
                    {
                        try
                        {
                            WindowsCustom.EnableWindowsAnsiSequences();
                        }
                        catch
                        {
                            // suppress this exception, if it fails who cares.
                        }

                        _consolePrepped = true;
                    }
                }

                _consolePrepped = true;
            }
        }

        public void Trace(string message)
        {
            // todo: slows everything to a crawl?
            // WriteFor(LogLevel.Trace, message);
        }

        public void Debug(string message)
        {
            WriteFor(LogLevel.Debug, message);
        }

        public void Info(string message)
        {
            WriteFor(LogLevel.Info, message);
        }

        public void Warn(string message)
        {
            WriteFor(LogLevel.Warn, message);
        }

        public void Error(string message)
        {
            WriteFor(LogLevel.Error, message);
        }

        public void Fatal(string message)
        {
            WriteFor(LogLevel.Fatal, message);
        }

        private void WriteFor(in LogLevel level, in string message)
        {
            var outputMessage = $"[{level.LogCode()}] :: {_name} :: {message}";
            if ((int) level >= (int) _outputLevel)
            {
                Console.WriteLine(outputMessage);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(outputMessage);
            }
        }
    }
}