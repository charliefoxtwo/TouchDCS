namespace Core.Logging
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    public static class LogLevelExtensions
    {
        public static string LogCode(this LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "\u001b[30;1mDEBUG\u001b[0m",
                LogLevel.Info => "\u001b[36;1m INFO\u001b[0m",
                LogLevel.Warn => "\u001b[33m WARN\u001b[0m",
                LogLevel.Error => "\u001b[31mERROR\u001b[0m",
                LogLevel.Fatal => "\u001b[7m\u001b[31mFATAL\u001b[0m",
                _ => "TRACE",
            };
        }
    }
}