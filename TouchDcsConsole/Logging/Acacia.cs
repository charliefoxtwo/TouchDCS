using System;
using Core.Logging;

namespace TouchDcsConsole.Logging
{
    public class Acacia : ILogger
    {
        private readonly string _name;
        private volatile LogLevel _outputLevel;

        public Acacia(in string name,  in LogLevel outputLevel = LogLevel.Warn)
        {
            _name = name;
            _outputLevel = outputLevel;
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

        public void SetLogLevel(LogLevel level)
        {
            _outputLevel = level;
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