using System.Runtime.InteropServices;

namespace Cpu.Tui.Input;

internal static class UnixTerminalRawMode
{
    private const uint ICANON = 2;
    private const uint ECHO = 8;
    private const int Tcsadrain = 1;
    private static readonly object Gate = new();
    private static byte[]? _savedCc;
    private static bool _active;

    public static void Enter()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        lock (Gate)
        {
            if (_active)
                return;

            int fd = StdinFd();
            if (fd < 0)
                return;

            var current = CreateTermios();
            if (tcgetattr(fd, ref current) != 0)
                return;

            _savedCc = (byte[])current.c_cc.Clone();

            var raw = current;
            raw.c_lflag &= ~(ICANON | ECHO);
            raw.c_cc[VMinIndex()] = 0;
            raw.c_cc[VTimeIndex()] = 0;

            if (tcsetattr(fd, Tcsadrain, ref raw) != 0)
                return;

            _active = true;
        }
    }

    public static void Exit()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        lock (Gate)
        {
            if (!_active || _savedCc == null)
                return;

            int fd = StdinFd();
            if (fd < 0)
                return;

            var restore = CreateTermios();
            if (tcgetattr(fd, ref restore) != 0)
                return;

            restore.c_cc = (byte[])_savedCc.Clone();
            tcsetattr(fd, Tcsadrain, ref restore);
            _active = false;
            _savedCc = null;
        }
    }

    private static int VMinIndex() => OperatingSystem.IsMacOS() ? 16 : 6;

    private static int VTimeIndex() => OperatingSystem.IsMacOS() ? 17 : 5;

    private static int StdinFd() => 0;

    private static Termios CreateTermios() => new() { c_cc = new byte[32] };

    [StructLayout(LayoutKind.Sequential)]
    private struct Termios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] c_cc;
        public uint c_ispeed;
        public uint c_ospeed;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, ref Termios termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optionalActions, ref Termios termios);
}
