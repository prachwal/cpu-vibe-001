namespace Cpu.Apple1.Devices;

/// <summary>
/// Apple 1 host keyboard ↔ machine key codes.
/// Single source for input translation and reference UI.
/// </summary>
public static class Apple1HostKeyMap
{
    public static IReadOnlyList<Apple1HostKeyBinding> SpecialKeys { get; } =
    [
        new("Return", Apple1KeyboardCodes.Return, "Enter",
            MapsFromHost: true, HostChar: '\r', HostKey: ConsoleKey.Enter),
        new("Delete", Apple1KeyboardCodes.DeleteLeft, "Backspace",
            MapsFromHost: true, HostChar: '\b', HostKey: ConsoleKey.Backspace),
        new("Delete", Apple1KeyboardCodes.DeleteLeft, "Delete",
            MapsFromHost: true, HostChar: '\x7F', HostKey: ConsoleKey.Delete),
        new("Escape", Apple1KeyboardCodes.Escape, "Esc",
            MapsFromHost: true, HostKey: ConsoleKey.Escape),
        new("Block XAM", Apple1KeyboardCodes.BlockExamine, ".",
            MapsFromHost: true, HostChar: '.', WozMonitorOnly: true),
        new("Store", Apple1KeyboardCodes.StoreMode, ":",
            MapsFromHost: true, HostChar: ':', WozMonitorOnly: true),
        new("Run", Apple1KeyboardCodes.Run, "R",
            MapsFromHost: true, HostChar: 'R', WozMonitorOnly: true),
        new("Run", Apple1KeyboardCodes.Run, "r",
            MapsFromHost: true, HostChar: 'r', WozMonitorOnly: true),
        new("Hex 0-9", Apple1KeyboardCodes.DigitBase, "0-9 (Woz)",
            WozMonitorOnly: true),
        new("Hex A-F", Apple1KeyboardCodes.HexLetterBase, "A-F (Woz)",
            WozMonitorOnly: true),
    ];

    public static IReadOnlyList<Apple1HostKeyBinding> PanelRows { get; } =
        SpecialKeys
            .GroupBy(b => b.MachineCode)
            .Select(g =>
            {
                Apple1HostKeyBinding first = g.First();
                string hosts = string.Join(", ", g.Select(x => x.HostLabel).Distinct());
                return first with { HostLabel = hosts };
            })
            .OrderBy(b => b.MachineCode)
            .ToArray();

    public static bool TryMapHostChar(char ch, bool wozMonitor, out byte machineCode)
    {
        foreach (Apple1HostKeyBinding binding in SpecialKeys)
        {
            if (!binding.MapsFromHost || binding.HostChar != ch)
                continue;
            if (binding.WozMonitorOnly && !wozMonitor)
                continue;
            machineCode = binding.MachineCode;
            return true;
        }

        if (wozMonitor)
            return TryMapWozPrintable(ch, out machineCode);

        if (ch >= 'a' && ch <= 'z')
        {
            machineCode = (byte)(ch - 0x20);
            return true;
        }

        if (ch >= 0x20 && ch < 0x7F)
        {
            machineCode = (byte)ch;
            return true;
        }

        machineCode = 0;
        return false;
    }

    public static bool TryMapConsoleKey(ConsoleKeyInfo key, bool wozMonitor, out byte machineCode)
    {
        foreach (Apple1HostKeyBinding binding in SpecialKeys)
        {
            if (!binding.MapsFromHost)
                continue;
            if (binding.WozMonitorOnly && !wozMonitor)
                continue;

            if (binding.HostKey == key.Key)
            {
                machineCode = binding.MachineCode;
                return true;
            }

            if (binding.HostChar.HasValue && key.KeyChar == binding.HostChar.Value)
            {
                machineCode = binding.MachineCode;
                return true;
            }
        }

        machineCode = 0;
        return false;
    }

    public static string FormatPanelLine(Apple1HostKeyBinding binding, int width)
    {
        string label = binding.MachineLabel;
        if (label.Length > 10)
            label = label[..10];

        string code = $"${binding.MachineCode:X2}";
        string host = binding.HostLabel;
        int hostW = Math.Max(1, width - 12);
        if (host.Length > hostW)
            host = host[..hostW];

        string line = string.Format("{0,-" + hostW + "} {1,10} {2}", host, label, code);
        if (line.Length > width)
            line = line[..width];
        else if (line.Length < width)
            line += new string(' ', width - line.Length);
        return line;
    }

    private static bool TryMapWozPrintable(char ch, out byte machineCode)
    {
        if (ch >= '0' && ch <= '9')
        {
            machineCode = (byte)(Apple1KeyboardCodes.DigitBase + (ch - '0'));
            return true;
        }

        if (ch is >= 'A' and <= 'F')
        {
            machineCode = (byte)(Apple1KeyboardCodes.HexLetterBase + (ch - 'A'));
            return true;
        }

        if (ch is >= 'a' and <= 'f')
        {
            machineCode = (byte)(Apple1KeyboardCodes.HexLetterBase + (ch - 'a'));
            return true;
        }

        if (ch >= 'a' && ch <= 'z')
        {
            machineCode = (byte)(ch - 0x20);
            return true;
        }

        if (ch >= 0x20 && ch < 0x7F)
        {
            machineCode = (byte)ch;
            return true;
        }

        machineCode = 0;
        return false;
    }
}
