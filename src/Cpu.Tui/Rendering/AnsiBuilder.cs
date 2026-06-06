namespace Cpu.Tui.Rendering;

/// <summary>
/// Zero-allocation ANSI escape sequence builder.
/// Writes directly to a byte[] buffer.
/// </summary>
public ref struct AnsiBuilder
{
    private readonly Span<byte> _buffer;
    private int _length;

    public int Length => _length;

    public AnsiBuilder(Span<byte> buffer)
    {
        _buffer = buffer;
        _length = 0;
    }

    /// <summary>
    /// ESC[{row+1};{col+1}H — move cursor.
    /// </summary>
    public void MoveTo(int col, int row)
    {
        WriteByte(0x1B);
        WriteByte((byte)'[');
        WriteInt(row + 1);
        WriteByte((byte)';');
        WriteInt(col + 1);
        WriteByte((byte)'H');
    }

    /// <summary>
    /// ESC[{fg};{bg}m — set SGR colors if changed.
    /// </summary>
    public void SetColor(ConsoleColor fg, ConsoleColor bg, ref ConsoleColor curFg, ref ConsoleColor curBg)
    {
        if (fg == curFg && bg == curBg) return;

        WriteByte(0x1B);
        WriteByte((byte)'[');

        bool needFg = fg != curFg;
        bool needBg = bg != curBg;

        if (needFg) WriteInt(ForegroundSgr(fg));
        if (needFg && needBg) WriteByte((byte)';');
        if (needBg) WriteInt(BackgroundSgr(bg));

        WriteByte((byte)'m');

        curFg = fg;
        curBg = bg;
    }

    /// <summary>
    /// ConsoleColor → ANSI SGR foreground code.
    /// </summary>
    private static int ForegroundSgr(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => 30,
        ConsoleColor.DarkBlue => 34,
        ConsoleColor.DarkGreen => 32,
        ConsoleColor.DarkCyan => 36,
        ConsoleColor.DarkRed => 31,
        ConsoleColor.DarkMagenta => 35,
        ConsoleColor.DarkYellow => 33,
        ConsoleColor.Gray => 37,
        ConsoleColor.DarkGray => 90,
        ConsoleColor.Blue => 94,
        ConsoleColor.Green => 92,
        ConsoleColor.Cyan => 96,
        ConsoleColor.Red => 91,
        ConsoleColor.Magenta => 95,
        ConsoleColor.Yellow => 93,
        ConsoleColor.White => 97,
        _ => 37
    };

    /// <summary>
    /// ConsoleColor → ANSI SGR background code.
    /// </summary>
    private static int BackgroundSgr(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => 40,
        ConsoleColor.DarkBlue => 44,
        ConsoleColor.DarkGreen => 42,
        ConsoleColor.DarkCyan => 46,
        ConsoleColor.DarkRed => 41,
        ConsoleColor.DarkMagenta => 45,
        ConsoleColor.DarkYellow => 43,
        ConsoleColor.Gray => 47,
        ConsoleColor.DarkGray => 100,
        ConsoleColor.Blue => 104,
        ConsoleColor.Green => 102,
        ConsoleColor.Cyan => 106,
        ConsoleColor.Red => 101,
        ConsoleColor.Magenta => 105,
        ConsoleColor.Yellow => 103,
        ConsoleColor.White => 107,
        _ => 40
    };

    /// <summary>
    /// Write a single UTF-8 char.
    /// </summary>
    public void WriteChar(char ch)
    {
        if (ch <= 0x7F)
        {
            WriteByte((byte)ch);
        }
        else if (ch <= 0x7FF)
        {
            WriteByte((byte)(0xC0 | (ch >> 6)));
            WriteByte((byte)(0x80 | (ch & 0x3F)));
        }
        else
        {
            WriteByte((byte)(0xE0 | (ch >> 12)));
            WriteByte((byte)(0x80 | ((ch >> 6) & 0x3F)));
            WriteByte((byte)(0x80 | (ch & 0x3F)));
        }
    }

    /// <summary>
    /// Write ASCII string.
    /// </summary>
    public void WriteAscii(ReadOnlySpan<char> text)
    {
        foreach (char c in text)
            WriteByte((byte)c);
    }

    /// <summary>
    /// Write raw byte.
    /// </summary>
    public void WriteByte(byte b)
    {
        if (_length < _buffer.Length)
            _buffer[_length++] = b;
    }

    /// <summary>
    /// Write raw bytes.
    /// </summary>
    public void WriteRaw(ReadOnlySpan<byte> data)
    {
        int available = _buffer.Length - _length;
        int toWrite = Math.Min(data.Length, available);
        data.Slice(0, toWrite).CopyTo(_buffer.Slice(_length));
        _length += toWrite;
    }

    private void WriteInt(int value)
    {
        Span<byte> temp = stackalloc byte[4];
        int len = 0;

        if (value >= 100)
        {
            temp[len++] = (byte)('0' + value / 100);
            value %= 100;
            temp[len++] = (byte)('0' + value / 10);
        }
        else if (value >= 10)
        {
            temp[len++] = (byte)('0' + value / 10);
        }
        temp[len++] = (byte)('0' + value % 10);

        for (int i = 0; i < len; i++)
            WriteByte(temp[i]);
    }
}
