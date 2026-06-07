using Cpu.Module;

namespace Cpu.Tui.Input;

public static class AnsiInputParser
{
    public static bool TryParse(ReadOnlySpan<byte> buffer, out int consumed, out TerminalInput input)
    {
        consumed = 0;
        input = default;

        if (buffer.Length == 0)
            return false;

        if (buffer[0] != 0x1b)
            return TryParsePlainKey(buffer, out consumed, out input);

        if (buffer.Length < 2)
            return false;

        if (buffer[1] == (byte)'[')
        {
            if (buffer.Length >= 3 && buffer[2] == (byte)'<')
            {
                if (TryParseSgrMouse(buffer, out consumed, out input))
                    return true;
            }

            if (buffer.Length >= 3 && buffer[2] == (byte)'M')
            {
                if (TryParseLegacyMouse(buffer, out consumed, out input))
                    return true;
            }

            if (TryParseCsiKey(buffer, out consumed, out input))
                return true;
        }

        if (buffer[1] == (byte)'O' && TryParseSs3Key(buffer, out consumed, out input))
            return true;

        consumed = 1;
        input = new TerminalInput(TerminalInputKind.Key,
            new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false));
        return true;
    }

    private static bool TryParsePlainKey(ReadOnlySpan<byte> buffer, out int consumed, out TerminalInput input)
    {
        consumed = 1;
        byte b = buffer[0];
        char ch = (char)b;
        ConsoleKey key = b switch
        {
            13 => ConsoleKey.Enter,
            8 => ConsoleKey.Backspace,
            127 => ConsoleKey.Backspace,
            _ => KeyFromChar(ch)
        };
        input = new TerminalInput(TerminalInputKind.Key, new ConsoleKeyInfo(ch, key, false, false, false));
        return true;
    }

    private static bool TryParseSgrMouse(ReadOnlySpan<byte> buffer, out int consumed, out TerminalInput input)
    {
        consumed = 0;
        input = default;

        int end = IndexOfTerminator(buffer, 3);
        if (end < 0)
            return false;

        ReadOnlySpan<byte> body = buffer.Slice(3, end - 3);
        if (!TrySplitMouseFields(body, out int cb, out int x, out int y))
            return false;

        char term = (char)buffer[end];
        consumed = end + 1;
        input = new TerminalInput(TerminalInputKind.Mouse, default, DecodeSgrMouse(cb, x, y, term == 'm'));
        return true;
    }

    private static bool TryParseLegacyMouse(ReadOnlySpan<byte> buffer, out int consumed, out TerminalInput input)
    {
        consumed = 0;
        input = default;

        if (buffer.Length < 6)
            return false;

        int cb = buffer[3] - 32;
        int x = buffer[4] - 32 - 1;
        int y = buffer[5] - 32 - 1;
        consumed = 6;
        bool release = (cb & 3) == 3;
        int button = cb & 3;
        bool motion = (cb & 32) != 0;
        input = new TerminalInput(TerminalInputKind.Mouse, default,
            new MouseEvent(x, y, button, release, motion));
        return true;
    }

    private static MouseEvent DecodeSgrMouse(int cb, int x, int y, bool isRelease)
    {
        x = Math.Max(0, x - 1);
        y = Math.Max(0, y - 1);

        if (cb == 35)
            return new MouseEvent(x, y, 0, false, true);

        if (cb >= 32 && cb <= 34)
            return new MouseEvent(x, y, cb - 32, false, true);

        if (cb >= 64)
            return new MouseEvent(x, y, cb, false, true);

        return new MouseEvent(x, y, cb, isRelease, false);
    }

    private static bool TryParseCsiKey(ReadOnlySpan<byte> buffer, out int consumed, out TerminalInput input)
    {
        consumed = 0;
        input = default;

        int i = 2;
        while (i < buffer.Length)
        {
            byte b = buffer[i];
            if (b >= (byte)'A' && b <= (byte)'Z')
            {
                consumed = i + 1;
                input = new TerminalInput(TerminalInputKind.Key, MapCsiFinal((char)b, buffer.Slice(2, i - 2)));
                return true;
            }

            if (b == (byte)'~')
            {
                consumed = i + 1;
                if (TryParseTildeKey(buffer.Slice(2, i - 2), out ConsoleKeyInfo key))
                {
                    input = new TerminalInput(TerminalInputKind.Key, key);
                    return true;
                }
                return false;
            }

            i++;
        }

        return false;
    }

    private static bool TryParseSs3Key(ReadOnlySpan<byte> buffer, out int consumed, out TerminalInput input)
    {
        consumed = 0;
        input = default;

        if (buffer.Length < 3)
            return false;

        consumed = 3;
        ConsoleKey key = (char)buffer[2] switch
        {
            'P' => ConsoleKey.F1,
            'Q' => ConsoleKey.F2,
            'R' => ConsoleKey.F3,
            'S' => ConsoleKey.F4,
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            'C' => ConsoleKey.RightArrow,
            'D' => ConsoleKey.LeftArrow,
            'H' => ConsoleKey.Home,
            'F' => ConsoleKey.End,
            _ => ConsoleKey.Oem1
        };
        input = new TerminalInput(TerminalInputKind.Key, new ConsoleKeyInfo('\0', key, false, false, false));
        return true;
    }

    private static ConsoleKeyInfo MapCsiFinal(char final, ReadOnlySpan<byte> paramsSpan)
    {
        ConsoleKey key = final switch
        {
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            'C' => ConsoleKey.RightArrow,
            'D' => ConsoleKey.LeftArrow,
            'H' => ConsoleKey.Home,
            'F' => ConsoleKey.End,
            _ => ConsoleKey.Oem1
        };
        return new ConsoleKeyInfo('\0', key, false, false, false);
    }

    private static bool TryParseTildeKey(ReadOnlySpan<byte> paramsSpan, out ConsoleKeyInfo key)
    {
        key = default;
        if (!TryParseLeadingInt(paramsSpan, out int code))
            return false;

        ConsoleKey k = code switch
        {
            1 => ConsoleKey.Home,
            2 => ConsoleKey.Insert,
            3 => ConsoleKey.Delete,
            4 => ConsoleKey.End,
            5 => ConsoleKey.PageUp,
            6 => ConsoleKey.PageDown,
            11 => ConsoleKey.F1,
            12 => ConsoleKey.F2,
            13 => ConsoleKey.F3,
            14 => ConsoleKey.F4,
            15 => ConsoleKey.F5,
            17 => ConsoleKey.F6,
            18 => ConsoleKey.F7,
            19 => ConsoleKey.F8,
            20 => ConsoleKey.F9,
            21 => ConsoleKey.F10,
            23 => ConsoleKey.F11,
            24 => ConsoleKey.F12,
            _ => ConsoleKey.Oem1
        };
        key = new ConsoleKeyInfo('\0', k, false, false, false);
        return true;
    }

    private static int IndexOfTerminator(ReadOnlySpan<byte> buffer, int start)
    {
        for (int i = start; i < buffer.Length; i++)
        {
            byte b = buffer[i];
            if (b == (byte)'M' || b == (byte)'m')
                return i;
        }
        return -1;
    }

    private static bool TrySplitMouseFields(ReadOnlySpan<byte> body, out int cb, out int x, out int y)
    {
        cb = x = y = 0;
        int part = 0;
        int value = 0;
        bool hasDigit = false;

        for (int i = 0; i <= body.Length; i++)
        {
            if (i < body.Length && body[i] >= (byte)'0' && body[i] <= (byte)'9')
            {
                value = value * 10 + (body[i] - (byte)'0');
                hasDigit = true;
                continue;
            }

            if (i < body.Length && body[i] != (byte)';')
                return false;

            if (!hasDigit)
                return false;

            switch (part)
            {
                case 0: cb = value; break;
                case 1: x = value; break;
                case 2: y = value; break;
                default: return false;
            }

            part++;
            value = 0;
            hasDigit = false;
        }

        return part == 3;
    }

    private static bool TryParseLeadingInt(ReadOnlySpan<byte> span, out int value)
    {
        value = 0;
        if (span.Length == 0)
            return false;

        int i = 0;
        while (i < span.Length && span[i] >= (byte)'0' && span[i] <= (byte)'9')
        {
            value = value * 10 + (span[i] - (byte)'0');
            i++;
        }

        return i > 0;
    }

    private static ConsoleKey KeyFromChar(char ch) =>
        char.IsLetter(ch) ? (ConsoleKey)char.ToUpperInvariant(ch) : ConsoleKey.Oem1;
}
