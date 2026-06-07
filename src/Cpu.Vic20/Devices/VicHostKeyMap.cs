namespace Cpu.Vic20.Devices;

public sealed record VicHostKeyBinding(
    string Label,
    int Row,
    int Col,
    string HostLabel,
    bool MapsFromHost = false,
    char? HostChar = null,
    ConsoleKey? HostKey = null);

public static class VicHostKeyMap
{
    public static IReadOnlyList<VicHostKeyBinding> SpecialKeys { get; } =
    [
        new("Return", 3, 0, "Enter", true, '\r', ConsoleKey.Enter),
        new("Return", 3, 0, "Enter key", true, null, ConsoleKey.Enter),
        new("Left", 3, 1, "Left", true, null, ConsoleKey.LeftArrow),
        new("Right", 3, 2, "Right", true, null, ConsoleKey.RightArrow),
        new("Down", 3, 3, "Down", true, null, ConsoleKey.DownArrow),
        new("Up", 3, 4, "Up", true, null, ConsoleKey.UpArrow),
        new("Run/Stop", 3, 5, "F3", true, null, ConsoleKey.F3),
        new("Restore", 3, 6, "F5", true, null, ConsoleKey.F5),
        new("Space", 4, 0, "Space", true, ' ', ConsoleKey.Spacebar),
        new("Stop", 4, 3, "Esc", true, null, ConsoleKey.Escape),
    ];

    public static IReadOnlyList<VicHostKeyBinding> PanelRows { get; } =
    [
        new("Return", 3, 0, "Enter"),
        new("Arrows", 3, 1, "Arrows"),
        new("Run/Stop", 3, 5, "F3"),
        new("Restore", 3, 6, "F5"),
        new("Space", 4, 0, "Space"),
        new("Shift", 4, 1, "Shift"),
        new("CBM", 4, 2, "Ctrl"),
        new("Stop", 4, 3, "Esc"),
    ];

    private static readonly (int Row, int Col, char Char)[] LetterMap =
    [
        (0, 0, '@'), (0, 1, 'A'), (0, 2, 'B'), (0, 3, 'C'), (0, 4, 'D'), (0, 5, 'E'), (0, 6, 'F'), (0, 7, 'G'),
        (1, 0, 'H'), (1, 1, 'I'), (1, 2, 'J'), (1, 3, 'K'), (1, 4, 'L'), (1, 5, 'M'), (1, 6, 'N'), (1, 7, 'O'),
        (2, 0, 'P'), (2, 1, 'Q'), (2, 2, 'R'), (2, 3, 'S'), (2, 4, 'T'), (2, 5, 'U'), (2, 6, 'V'), (2, 7, 'W'),
        (5, 0, 'X'), (5, 1, 'Y'), (5, 2, 'Z'), (5, 3, '['), (5, 4, '\\'), (5, 5, ']'), (5, 6, '^'), (5, 7, '_'),
        (6, 0, '0'), (6, 1, '1'), (6, 2, '2'), (6, 3, '3'), (6, 4, '4'), (6, 5, '5'), (6, 6, '6'), (6, 7, '7'),
        (7, 0, '8'), (7, 1, '9'), (7, 2, ':'), (7, 3, ';'), (7, 4, ','), (7, 5, '-'), (7, 6, '.'), (7, 7, '/'),
    ];

    public static bool TryMapConsoleKey(ConsoleKeyInfo key, out int row, out int col)
    {
        foreach (VicHostKeyBinding binding in SpecialKeys)
        {
            if (!binding.MapsFromHost)
                continue;
            if (binding.HostKey == key.Key)
            {
                row = binding.Row;
                col = binding.Col;
                return true;
            }
            if (binding.HostChar.HasValue && key.KeyChar == binding.HostChar.Value)
            {
                row = binding.Row;
                col = binding.Col;
                return true;
            }
        }

        char upper = char.ToUpperInvariant(key.KeyChar);
        if (upper >= 'A' && upper <= 'Z')
        {
            foreach ((int r, int c, char letter) in LetterMap)
            {
                if (letter == upper)
                {
                    row = r;
                    col = c;
                    return true;
                }
            }
        }

        foreach ((int r, int c, char letter) in LetterMap)
        {
            if (letter == key.KeyChar)
            {
                row = r;
                col = c;
                return true;
            }
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            row = 4;
            col = 1;
            return true;
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            row = 4;
            col = 2;
            return true;
        }

        row = 0;
        col = 0;
        return false;
    }

    public static string FormatPanelLine(VicHostKeyBinding binding, int width)
    {
        string host = binding.HostLabel;
        if (host.Length > 12)
            host = host[..12];
        string line = $"{host,-12} {binding.Label,8}";
        if (line.Length > width)
            line = line[..width];
        else if (line.Length < width)
            line += new string(' ', width - line.Length);
        return line;
    }
}
