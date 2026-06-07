namespace Cpu.C16.Devices;

public static class C16HostKeyMap
{
    private static readonly (int Row, int Col, char Char, byte Petscii)[] LetterMap =
    [
        (1, 1, 'A', 0x41), (1, 2, 'S', 0x53), (1, 3, 'D', 0x44), (1, 4, 'F', 0x46),
        (1, 5, 'G', 0x47), (1, 6, 'H', 0x48), (1, 7, 'J', 0x4A),
        (2, 0, 'K', 0x4B), (2, 1, 'L', 0x4C), (2, 7, 'Z', 0x5A), (2, 6, 'X', 0x58),
        (2, 5, 'C', 0x43), (2, 4, 'V', 0x56), (2, 3, 'B', 0x42),
        (3, 2, 'N', 0x4E), (3, 3, 'M', 0x4D), (3, 0, 'P', 0x50), (3, 1, 'Q', 0x51),
        (3, 4, 'R', 0x52), (3, 5, 'S', 0x53), (3, 6, 'T', 0x54),
        (3, 7, 'U', 0x55), (4, 0, 'W', 0x57), (4, 1, 'X', 0x58), (4, 2, 'Y', 0x59),
        (4, 3, 'Z', 0x5A),
        (4, 4, ' ', 0x20),
        (5, 0, '0', 0x30), (5, 1, '1', 0x31), (5, 2, '2', 0x32), (5, 3, '3', 0x33),
        (5, 4, '4', 0x34),
        (6, 0, '5', 0x35), (6, 1, '6', 0x36), (6, 2, '7', 0x37), (6, 3, '8', 0x38),
        (6, 4, '9', 0x39),
        (7, 0, '+', 0x2B), (7, 1, '-', 0x2D), (7, 2, '@', 0x40), (7, 3, ':', 0x3A),
        (7, 4, ';', 0x3B), (7, 5, ',', 0x2C), (7, 6, '.', 0x2E), (7, 7, '/', 0x2F),
    ];

    private static readonly Dictionary<ConsoleKey, (int Row, int Col, byte Code)> SpecialKeys = new()
    {
        [ConsoleKey.Enter] = (3, 1, 0x0D),
        [ConsoleKey.Backspace] = (1, 7, 0x08),
        [ConsoleKey.Delete] = (1, 7, 0x7F),
    };

    public static bool TryMapHostChar(char ch, out int row, out int col)
    {
        char upper = char.ToUpperInvariant(ch);
        foreach ((int r, int c, char letter, byte _) in LetterMap)
        {
            if (letter == upper || letter == ch)
            {
                row = r; col = c;
                return true;
            }
        }
        row = 0; col = 0;
        return false;
    }

    public static bool TryMapConsoleKey(ConsoleKeyInfo key, out int row, out int col, out byte petscii)
    {
        if (SpecialKeys.TryGetValue(key.Key, out var spec))
        {
            (row, col, petscii) = spec;
            return true;
        }
        if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F)
        {
            char upper = char.ToUpperInvariant(key.KeyChar);
            foreach ((int r, int c, char letter, byte p) in LetterMap)
            {
                if (letter == upper || letter == key.KeyChar)
                {
                    row = r; col = c; petscii = p;
                    return true;
                }
            }
        }
        row = 0; col = 0; petscii = 0;
        return false;
    }
}
