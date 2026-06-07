namespace Cpu.C16.Devices;

public sealed record C16HostKeyBinding(
    string Label,
    int Row,
    int Col,
    string HostLabel,
    char? HostChar = null,
    ConsoleKey? HostKey = null,
    byte? Petscii = null);

public static class C16HostKeyMap
{
    private static readonly C16HostKeyBinding[] Bindings =
    [
        new("Delete", 0, 0, "Backspace", '\b', ConsoleKey.Backspace, 0x14),
        new("Return", 0, 1, "Enter", '\r', ConsoleKey.Enter, 0x0D),
        new("Home", 7, 1, "Home", null, ConsoleKey.Home, 0x13),
        new("Space", 7, 4, "Space", ' ', ConsoleKey.Spacebar, 0x20),
        new("Left", 6, 0, "Left", null, ConsoleKey.LeftArrow, 0x9D),
        new("Right", 6, 3, "Right", null, ConsoleKey.RightArrow, 0x1D),
        new("Up", 5, 3, "Up", null, ConsoleKey.UpArrow, 0x91),
        new("Down", 5, 0, "Down", null, ConsoleKey.DownArrow, 0x11),
        new("Run/Stop", 7, 7, "F3", null, ConsoleKey.F3, 0x03),
        new("@", 0, 7, "@", '@', null, (byte)'@'),
        new("+", 6, 6, "+", '+', ConsoleKey.Add, (byte)'+'),
        new("-", 5, 6, "-", '-', ConsoleKey.Subtract, (byte)'-'),
        new("*", 6, 1, "*", '*', ConsoleKey.Multiply, (byte)'*'),
        new("/", 6, 7, "/", '/', ConsoleKey.Divide, (byte)'/'),
        new(",", 5, 7, ",", ',', ConsoleKey.OemComma, (byte)','),
        new(".", 5, 4, ".", '.', ConsoleKey.OemPeriod, (byte)'.'),
        new(":", 5, 5, ":", ':', null, (byte)':'),
        new(";", 6, 2, ";", ';', ConsoleKey.Oem1, (byte)';'),
        new("=", 6, 5, "=", '=', ConsoleKey.OemPlus, (byte)'='),
    ];

    private static readonly (int Row, int Col, char Char)[] LetterMap =
    [
        (1, 2, 'A'), (3, 4, 'B'), (2, 4, 'C'), (2, 2, 'D'), (1, 6, 'E'), (2, 5, 'F'), (3, 2, 'G'), (3, 5, 'H'),
        (4, 1, 'I'), (4, 2, 'J'), (4, 5, 'K'), (5, 2, 'L'), (4, 4, 'M'), (4, 7, 'N'), (4, 6, 'O'), (5, 1, 'P'),
        (7, 6, 'Q'), (2, 1, 'R'), (1, 5, 'S'), (2, 6, 'T'), (3, 6, 'U'), (3, 7, 'V'), (1, 1, 'W'), (2, 7, 'X'),
        (3, 1, 'Y'), (1, 4, 'Z'),
    ];

    private static readonly (int Row, int Col, char Char)[] DigitMap =
    [
        (4, 3, '0'), (7, 0, '1'), (7, 3, '2'), (1, 0, '3'), (1, 3, '4'),
        (2, 0, '5'), (2, 3, '6'), (3, 0, '7'), (3, 3, '8'), (4, 0, '9'),
    ];

    public static bool TryMapConsoleKey(ConsoleKeyInfo key, out int row, out int col, out byte petscii)
    {
        foreach (C16HostKeyBinding binding in Bindings)
        {
            if (binding.HostKey == key.Key || (binding.HostChar.HasValue && binding.HostChar.Value == key.KeyChar))
            {
                row = binding.Row;
                col = binding.Col;
                petscii = binding.Petscii ?? (byte)binding.Label[0];
                return true;
            }
        }

        if (TryResolveHostChar(key, out char ch) && TryMapHostChar(ch, out row, out col, out petscii))
            return true;

        row = 0;
        col = 0;
        petscii = 0;
        return false;
    }

    public static bool TryMapHostChar(char ch, out int row, out int col, out byte petscii)
    {
        char upper = char.ToUpperInvariant(ch);
        foreach ((int r, int c, char letter) in LetterMap)
        {
            if (letter == upper)
            {
                row = r;
                col = c;
                petscii = (byte)upper;
                return true;
            }
        }

        foreach ((int r, int c, char digit) in DigitMap)
        {
            if (digit == ch)
            {
                row = r;
                col = c;
                petscii = (byte)digit;
                return true;
            }
        }

        foreach (C16HostKeyBinding binding in Bindings)
        {
            if (binding.HostChar == ch)
            {
                row = binding.Row;
                col = binding.Col;
                petscii = binding.Petscii ?? (byte)ch;
                return true;
            }
        }

        row = 0;
        col = 0;
        petscii = 0;
        return false;
    }

    public static string? TryGetHostKeyLabel(ConsoleKeyInfo key)
    {
        foreach (C16HostKeyBinding binding in Bindings)
        {
            if (binding.HostKey == key.Key || (binding.HostChar.HasValue && binding.HostChar.Value == key.KeyChar))
                return binding.Label;
        }

        if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F)
            return new string(key.KeyChar, 1);

        if (key.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
            return ((char)('A' + (key.Key - ConsoleKey.A))).ToString();

        return null;
    }

    private static bool TryResolveHostChar(ConsoleKeyInfo key, out char ch)
    {
        if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F)
        {
            ch = key.KeyChar;
            return true;
        }

        if (key.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            ch = (char)('A' + (key.Key - ConsoleKey.A));
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
            ConsoleKey.Add => '+',
            ConsoleKey.Subtract => '-',
            ConsoleKey.Multiply => '*',
            ConsoleKey.Divide => '/',
            _ => '\0'
        };

        return ch != '\0';
    }
}
