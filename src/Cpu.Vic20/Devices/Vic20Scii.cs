namespace Cpu.Vic20.Devices;

public static class Vic20Scii
{
    public static char ToDisplayChar(byte code)
    {
        byte ch = (byte)(code & 0x7F);

        if (ch == 0)
            return ' ';

        if (ch is >= 0x01 and <= 0x1A)
            return (char)('A' + ch - 1);

        if (ch is >= 0x20 and <= 0x3F)
            return (char)ch;

        if (ch is >= 0x41 and <= 0x5A)
            return (char)('a' + ch - 0x41);

        if (ch is >= 0x5B and <= 0x5F)
            return (char)ch;

        return ch switch
        {
            0x40 => '@',
            0x60 => '`',
            0x0D or 0x0A => ' ',
            _ => ch >= 0x80 ? MapGraphics(ch) : ' '
        };
    }

    public static byte NormalizeScreenCode(byte value)
    {
        if (value is >= 0x41 and <= 0x5A)
            return (byte)(value - 0x40);
        return value;
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
