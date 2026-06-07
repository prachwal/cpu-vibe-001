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
        new("Left", 3, 1, "Backspace", true, '\b', ConsoleKey.Backspace),
        new("Left", 3, 1, "Delete", true, '\x7F', ConsoleKey.Delete),
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
        (4, 0, ' '),
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

        if (TryResolveHostChar(key, out char hostChar) && TryMapHostChar(hostChar, out row, out col))
            return true;

        row = 0;
        col = 0;
        return false;
    }

    public static bool TryMapHostChar(char ch, out int row, out int col)
    {
        char upper = char.ToUpperInvariant(ch);
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
            if (letter == ch)
            {
                row = r;
                col = c;
                return true;
            }
        }

        row = 0;
        col = 0;
        return false;
    }

    public static bool TryResolveHostChar(ConsoleKeyInfo key, out char ch)
    {
        if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F)
        {
            ch = key.KeyChar;
            return true;
        }

        if (key.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            ch = (char)('A' + (key.Key - ConsoleKey.A));
            if (!key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                ch = char.ToLowerInvariant(ch);
            return true;
        }

        if (key.Key is >= ConsoleKey.D0 and <= ConsoleKey.D9)
        {
            ch = (char)('0' + (key.Key - ConsoleKey.D0));
            return true;
        }

        ch = key.Key switch
        {
            ConsoleKey.OemMinus => '-',
            ConsoleKey.OemPlus => '+',
            ConsoleKey.OemComma => ',',
            ConsoleKey.OemPeriod => '.',
            ConsoleKey.Oem1 => ';',
            ConsoleKey.Oem2 => '/',
            ConsoleKey.Oem3 => '`',
            ConsoleKey.Oem4 => '[',
            ConsoleKey.Oem5 => '\\',
            ConsoleKey.Oem6 => ']',
            ConsoleKey.Oem7 => '\'',
            ConsoleKey.Oem8 => ',',
            ConsoleKey.Oem102 => '\\',
            ConsoleKey.Add => '+',
            ConsoleKey.Subtract => '-',
            ConsoleKey.Multiply => '*',
            ConsoleKey.Divide => '/',
            _ => '\0'
        };

        return ch != '\0';
    }

    private static readonly Dictionary<(int Row, int Col), byte> PetsciiMap = BuildPetsciiMap();

    private static Dictionary<(int, int), byte> BuildPetsciiMap()
    {
        var map = new Dictionary<(int, int), byte>();
        foreach ((int r, int c, char ch) in LetterMap)
            map[(r, c)] = ch >= 'a' && ch <= 'z'
                ? (byte)char.ToUpperInvariant(ch)
                : (byte)ch;
        map[(3, 0)] = 0x0D;
        map[(4, 0)] = 0x20;
        map[(3, 1)] = 0x9D;
        map[(3, 2)] = 0x1D;
        map[(3, 3)] = 0x11;
        map[(3, 4)] = 0x91;
        map[(3, 5)] = 0x83;
        map[(4, 3)] = 0x03;
        return map;
    }

    public static bool TryGetPetscii(int row, int col, out byte petscii)
    {
        return PetsciiMap.TryGetValue((row, col), out petscii);
    }

    public static string? TryGetHostKeyLabel(ConsoleKeyInfo key)
    {
        foreach (VicHostKeyBinding binding in SpecialKeys)
        {
            if (!binding.MapsFromHost)
                continue;
            if (binding.HostKey == key.Key)
                return binding.Label;
            if (binding.HostChar.HasValue && key.KeyChar == binding.HostChar.Value)
                return binding.Label;
        }

        if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F)
            return new string(key.KeyChar, 1);

        if (key.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
            return ((char)('A' + (key.Key - ConsoleKey.A))).ToString();

        return null;
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
