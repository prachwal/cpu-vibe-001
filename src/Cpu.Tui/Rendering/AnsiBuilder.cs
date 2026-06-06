namespace Cpu.Tui.Rendering;

/// <summary>
/// Zero-allocation ANSI escape sequence builder.
/// Supports both ConsoleColor and truecolor 24-bit RGB.
/// Detects buffer overflow — caller must check Overflow and flush.
/// </summary>
public ref struct AnsiBuilder
{
    private readonly Span<byte> _buffer;
    private int _length;
    private bool _overflowed;

    public int Length => _length;
    public bool Overflow => _overflowed;

    public AnsiBuilder(Span<byte> buffer)
    {
        _buffer = buffer;
        _length = 0;
        _overflowed = false;
    }

    public void Reset()
    {
        _length = 0;
        _overflowed = false;
    }

    /// <summary>
    /// Reserve space for worst-case cell: MoveTo ~12B + SetColor(fg+bg RGB) ~50B + char ~3B = ~65B
    /// Use 128 to be safe.
    /// </summary>
    public const int MaxCellBytes = 128;

    public void MoveTo(int col, int row)
    {
        WriteByte(0x1B);
        WriteByte((byte)'[');
        WriteInt(row + 1);
        WriteByte((byte)';');
        WriteInt(col + 1);
        WriteByte((byte)'H');
    }

    public void SetColor(ConsoleColor fg, ConsoleColor bg, ref ConsoleColor curFg, ref ConsoleColor curBg)
    {
        if (fg == curFg && bg == curBg) return;
        if (_overflowed) return;

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

    public void SetColor(TerminalColor fg, TerminalColor bg, ref TerminalColor curFg, ref TerminalColor curBg)
    {
        if (fg == curFg && bg == curBg) return;
        if (_overflowed) return;

        WriteByte(0x1B);
        WriteByte((byte)'[');

        bool needFg = fg != curFg;
        bool needBg = bg != curBg;

        if (needFg)
        {
            if (fg.IsRgb)
            {
                WriteByte((byte)'3'); WriteByte((byte)'8'); WriteByte((byte)';');
                WriteByte((byte)'2'); WriteByte((byte)';');
                WriteInt(fg.R); WriteByte((byte)';');
                WriteInt(fg.G); WriteByte((byte)';');
                WriteInt(fg.B);
            }
            else
            {
                WriteInt(ForegroundSgr(fg.ConsoleColor));
            }
        }

        if (needFg && needBg) WriteByte((byte)';');

        if (needBg)
        {
            if (bg.IsRgb)
            {
                WriteByte((byte)'4'); WriteByte((byte)'8'); WriteByte((byte)';');
                WriteByte((byte)'2'); WriteByte((byte)';');
                WriteInt(bg.R); WriteByte((byte)';');
                WriteInt(bg.G); WriteByte((byte)';');
                WriteInt(bg.B);
            }
            else
            {
                WriteInt(BackgroundSgr(bg.ConsoleColor));
            }
        }

        WriteByte((byte)'m');
        curFg = fg;
        curBg = bg;
    }

    public void WriteChar(char ch)
    {
        if (_overflowed) return;
        if (ch <= 0x7F)
            WriteByte((byte)ch);
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

    public void WriteAscii(ReadOnlySpan<char> text)
    {
        foreach (char c in text)
            WriteByte((byte)c);
    }

    public void WriteRaw(ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < data.Length; i++)
            WriteByte(data[i]);
    }

    public void WriteByte(byte b)
    {
        if (_overflowed) return;
        if (_length >= _buffer.Length)
        {
            _overflowed = true;
            return;
        }
        _buffer[_length++] = b;
    }

    public void WriteInt(int value)
    {
        Span<byte> temp = stackalloc byte[4];
        int len = 0;
        if (value >= 100) { temp[len++] = (byte)('0' + value / 100); value %= 100; temp[len++] = (byte)('0' + value / 10); }
        else if (value >= 10) { temp[len++] = (byte)('0' + value / 10); }
        temp[len++] = (byte)('0' + value % 10);
        for (int i = 0; i < len; i++) WriteByte(temp[i]);
    }

    private static int ForegroundSgr(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => 30, ConsoleColor.DarkBlue => 34, ConsoleColor.DarkGreen => 32,
        ConsoleColor.DarkCyan => 36, ConsoleColor.DarkRed => 31, ConsoleColor.DarkMagenta => 35,
        ConsoleColor.DarkYellow => 33, ConsoleColor.Gray => 37, ConsoleColor.DarkGray => 90,
        ConsoleColor.Blue => 94, ConsoleColor.Green => 92, ConsoleColor.Cyan => 96,
        ConsoleColor.Red => 91, ConsoleColor.Magenta => 95, ConsoleColor.Yellow => 93, ConsoleColor.White => 97,
        _ => 37
    };

    private static int BackgroundSgr(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => 40, ConsoleColor.DarkBlue => 44, ConsoleColor.DarkGreen => 42,
        ConsoleColor.DarkCyan => 46, ConsoleColor.DarkRed => 41, ConsoleColor.DarkMagenta => 45,
        ConsoleColor.DarkYellow => 43, ConsoleColor.Gray => 47, ConsoleColor.DarkGray => 100,
        ConsoleColor.Blue => 104, ConsoleColor.Green => 102, ConsoleColor.Cyan => 106,
        ConsoleColor.Red => 101, ConsoleColor.Magenta => 105, ConsoleColor.Yellow => 103, ConsoleColor.White => 107,
        _ => 40
    };
}
