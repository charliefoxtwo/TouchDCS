using Core.Logging;

namespace TouchDcsTest
{
    public class TestLogger : ILogger
    {
        public delegate void OnLog(string message);

        public event OnLog? OnTrace;
        public event OnLog? OnDebug;
        public event OnLog? OnInfo;
        public event OnLog? OnWarn;
        public event OnLog? OnError;
        public event OnLog? OnFatal;

        public void Trace(string message)
        {
            OnTrace?.Invoke(message);
        }

        public void Debug(string message)
        {
            OnDebug?.Invoke(message);
        }

        public void Info(string message)
        {
            OnInfo?.Invoke(message);
        }

        public void Warn(string message)
        {
            OnWarn?.Invoke(message);
        }

        public void Error(string message)
        {
            OnError?.Invoke(message);
        }

        public void Fatal(string message)
        {
            OnFatal?.Invoke(message);
        }
    }
}