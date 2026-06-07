namespace Cpu.Pet.Devices;

/// <summary>
/// PET screen code (PETSCII display charset) to terminal character mapping.
/// </summary>
public static class PetScii
{
    public static char ToDisplayChar(byte code)
    {
        if (code == 0)
            return ' ';

        if (code is >= 0x01 and <= 0x1A)
            return (char)('A' + code - 1);

        if (code is >= 0x20 and <= 0x3F)
            return (char)code;

        if (code is >= 0x41 and <= 0x5A)
            return (char)('a' + code - 0x41);

        if (code is >= 0x5B and <= 0x5F)
            return (char)code;

        return code switch
        {
            0x40 => '@',
            0x60 => '`',
            0x0D or 0x0A => ' ',
            _ => code >= 0x80 ? MapGraphics(code) : ' '
        };
    }

    public static byte NormalizeVideoRam(byte value)
    {
        if (value is >= 0x41 and <= 0x5A)
            return (byte)(value - 0x40);
        return value;
    }

    public static byte HostCharToKeyboardCode(char ch)
    {
        if (PetHostKeyMap.TryMapHostChar(ch, out byte special))
            return special;

        if (ch is >= 'a' and <= 'z')
            ch = char.ToUpperInvariant(ch);
        if (ch is >= 'A' and <= 'Z')
            return (byte)ch;
        if (ch is >= '0' and <= '9')
            return (byte)ch;
        if (ch >= '!' && ch <= '/')
            return (byte)ch;
        if (ch >= ':' && ch <= '?')
            return (byte)ch;
        return 0;
    }

    private static char MapGraphics(byte code)
    {
        return code switch
        {
            0xA0 => '▁',
            0xA1 => '▂',
            0xA2 => '▃',
            0xA3 => '▄',
            0xA4 => '▅',
            0xA5 => '▆',
            0xA6 => '▇',
            0xA7 => '█',
            0xB0 => '─',
            0xB1 => '│',
            0xB2 => '┌',
            0xB3 => '┐',
            0xB4 => '└',
            0xB5 => '┘',
            0xB6 => '├',
            0xB7 => '┤',
            0xB8 => '┬',
            0xB9 => '┴',
            0xBA => '┼',
            _ => '\u2591'
        };
    }
}
