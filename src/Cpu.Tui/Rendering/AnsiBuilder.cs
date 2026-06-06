using System.Text;

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

        if (needFg) WriteInt(30 + ConsoleColorToAnsi(fg));
        if (needFg && needBg) WriteByte((byte)';');
        if (needBg) WriteInt(40 + ConsoleColorToAnsi(bg));

        WriteByte((byte)'m');

        curFg = fg;
        curBg = bg;
    }

    /// <summary>
    /// ConsoleColor → ANSI color index (0-15).
    /// ConsoleColor and ANSI have different orderings.
    /// </summary>
    private static int ConsoleColorToAnsi(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => 0,
        ConsoleColor.DarkBlue => 4,
        ConsoleColor.DarkGreen => 2,
        ConsoleColor.DarkCyan => 6,
        ConsoleColor.DarkRed => 1,
        ConsoleColor.DarkMagenta => 5,
        ConsoleColor.DarkYellow => 3,
        ConsoleColor.Gray => 7,
        ConsoleColor.DarkGray => 8,
        ConsoleColor.Blue => 12,
        ConsoleColor.Green => 10,
        ConsoleColor.Cyan => 14,
        ConsoleColor.Red => 9,
        ConsoleColor.Magenta => 13,
        ConsoleColor.Yellow => 11,
        ConsoleColor.White => 15,
        _ => 7
    };

    /// <summary>
    /// Write a single ASCII char.
    /// </summary>
    public void WriteChar(char ch)
    {
        WriteByte((byte)ch);
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
