namespace Cpu.C16.Devices;

public static class C16HostKeyMap
{
    private static readonly (int Row, int Col, char Char, byte Petscii)[] LetterMap =
    [
        (0, 1, 'Z', 0x5A), (0, 2, 'X', 0x58), (0, 3, 'C', 0x43),
        (0, 4, 'V', 0x56), (0, 5, 'B', 0x42), (0, 6, 'N', 0x4E), (0, 7, 'M', 0x4D),
        (1, 1, 'A', 0x41), (1, 2, 'S', 0x53), (1, 3, 'D', 0x44),
        (1, 4, 'F', 0x46), (1, 5, 'G', 0x47), (1, 6, 'H', 0x48), (1, 7, 'J', 0x4A),
        (2, 0, 'K', 0x4B), (2, 1, 'L', 0x4C),
        (2, 6, ',', 0x2C), (2, 7, '-', 0x2D),
        (3, 1, 'Q', 0x51), (3, 2, 'E', 0x45), (3, 3, 'R', 0x52),
        (3, 4, 'T', 0x54), (3, 5, 'Y', 0x59), (3, 6, 'U', 0x55), (3, 7, 'I', 0x49),
        (4, 0, 'O', 0x4F), (4, 1, 'P', 0x50), (4, 2, '@', 0x40),
        (5, 1, '1', 0x31), (5, 2, '2', 0x32), (5, 3, '3', 0x33),
        (5, 4, '4', 0x34), (5, 5, '5', 0x35), (5, 6, '6', 0x36), (5, 7, '7', 0x37),
        (6, 0, '0', 0x30), (6, 1, '8', 0x38), (6, 2, '9', 0x39),
        (6, 5, ';', 0x3B), (6, 6, '.', 0x2E),
        (6, 7, 'W', 0x57),
        (7, 5, '/', 0x2F),
    ];

    private static readonly Dictionary<ConsoleKey, (int Row, int Col, byte Code)> SpecialKeys = new()
    {
        [ConsoleKey.Enter] = (2, 5, 0x0D),
        [ConsoleKey.Backspace] = (6, 6, 0x08),
        [ConsoleKey.Delete] = (6, 6, 0x7F),
        [ConsoleKey.Spacebar] = (1, 0, 0x20),
        [ConsoleKey.F1] = (7, 4, 0x04),
        [ConsoleKey.F2] = (7, 5, 0x05),
        [ConsoleKey.F3] = (7, 6, 0x06),
        [ConsoleKey.F4] = (7, 7, 0x07),
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
