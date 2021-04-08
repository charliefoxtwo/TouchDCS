using System;
using System.Runtime.InteropServices;

namespace TouchDcsConsole.Logging
{
    public static class WindowsCustom
    {
        private const int StdOutputHandle = -11;
        private const uint EnableVirtualTerminalProcessing = 0x0004;
        private const uint DisableNewlineAutoReturn = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        public static void EnableWindowsAnsiSequences()
        {
            var iStdOut = GetStdHandle(StdOutputHandle);
            if (!GetConsoleMode(iStdOut, out var outConsoleMode))
            {
                return;
            }

            outConsoleMode |= EnableVirtualTerminalProcessing | DisableNewlineAutoReturn;
            SetConsoleMode(iStdOut, outConsoleMode);
        }
    }
}