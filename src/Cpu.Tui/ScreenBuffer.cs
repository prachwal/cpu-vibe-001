using CpuBase;

namespace Cpu.Tui;

/// <summary>
/// Memory-mapped screen buffer.
/// Backed by IMemory-compatible byte array.
/// </summary>
public class ScreenBuffer : IMemory
{
    public const int Width = 80;
    public const int Height = 25;
    public const int CellCount = Width * Height;
    public const int CharBase = 0x0000;
    public const int AttrBase = 0x0800;
    public const int TotalSize = 0x1000;

    private readonly byte[] _data = new byte[TotalSize];
    private readonly bool[] _dirty = new bool[CellCount];
    private int _dirtyCount;

    public int DirtyCount => _dirtyCount;
    public bool IsDirty => _dirtyCount > 0;

    public byte Read(ushort address) => _data[address];

    public void Write(ushort address, byte value)
    {
        if (address < TotalSize) _data[address] = value;
    }

    public void Load(ushort address, byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            Write((ushort)(address + i), data[i]);
    }

    public void Reset()
    {
        Array.Clear(_data);
        Array.Clear(_dirty);
        _dirtyCount = 0;
    }

    public void ClearDirty()
    {
        if (_dirtyCount == 0) return;
        Array.Clear(_dirty);
        _dirtyCount = 0;
    }

    public bool IsCellDirty(int col, int row) => _dirty[row * Width + col];

    public char GetChar(int col, int row)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height) return ' ';
        return (char)_data[CharBase + row * Width + col];
    }

    public ConsoleColor GetForeground(int col, int row)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height)
            return ConsoleColor.Gray;

        byte attr = _data[AttrBase + row * Width + col];
        return AttrToConsoleColor((byte)(attr & 0x0F));
    }

    public ConsoleColor GetBackground(int col, int row)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height)
            return ConsoleColor.Black;

        byte attr = _data[AttrBase + row * Width + col];
        return AttrToConsoleColor((byte)((attr >> 4) & 0x0F));
    }

    public void SetCell(int col, int row, char ch, ConsoleColor? fg = null, ConsoleColor? bg = null)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height) return;
        int i = row * Width + col;
        _data[CharBase + i] = (byte)ch;
        _data[AttrBase + i] = ColorToAttr(fg ?? ConsoleColor.Gray, bg ?? ConsoleColor.Black);
        MarkDirty(i);
    }

    public void SetCellRaw(int col, int row, byte ch, byte attr)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height) return;
        int i = row * Width + col;
        _data[CharBase + i] = ch;
        _data[AttrBase + i] = attr;
        MarkDirty(i);
    }

    public void SetString(int col, int row, string text, ConsoleColor? fg = null, ConsoleColor? bg = null)
    {
        for (int i = 0; i < text.Length; i++)
            SetCell(col + i, row, text[i], fg, bg);
    }

    public void FillRect(int col, int row, int w, int h, char ch = ' ', ConsoleColor? fg = null, ConsoleColor? bg = null)
    {
        for (int r = row; r < row + h && r < Height; r++)
            for (int c = col; c < col + w && c < Width; c++)
                SetCell(c, r, ch, fg, bg);
    }

    /// <summary>
    /// Fast row fill — direct _data[] writes, no bounds checks per cell.
    /// </summary>
    public void FillRowFast(int row, byte ch, byte attr)
    {
        if (row < 0 || row >= Height) return;
        int charBase = CharBase + row * Width;
        int attrBase = AttrBase + row * Width;
        int rowStart = row * Width;
        for (int x = 0; x < Width; x++)
        {
            _data[charBase + x] = ch;
            _data[attrBase + x] = attr;
            _dirty[rowStart + x] = true;
        }
        _dirtyCount += Width;
    }

    /// <summary>
    /// Fast row copy — direct _data[] memcpy for char and attr planes.
    /// </summary>
    public void CopyRow(int srcRow, int dstRow)
    {
        if (srcRow < 0 || srcRow >= Height || dstRow < 0 || dstRow >= Height || srcRow == dstRow) return;
        int srcChar = CharBase + srcRow * Width;
        int dstChar = CharBase + dstRow * Width;
        int srcAttr = AttrBase + srcRow * Width;
        int dstAttr = AttrBase + dstRow * Width;
        int dstStart = dstRow * Width;

        Array.Copy(_data, srcChar, _data, dstChar, Width);
        Array.Copy(_data, srcAttr, _data, dstAttr, Width);
        for (int x = 0; x < Width; x++)
            _dirty[dstStart + x] = true;
        _dirtyCount += Width;
    }

    /// <summary>
    /// Get raw attr byte for a cell.
    /// </summary>
    public byte GetAttrRaw(int col, int row)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height) return 0;
        return _data[AttrBase + row * Width + col];
    }

    /// <summary>
    /// Get raw char byte for a cell.
    /// </summary>
    public byte GetRawChar(int col, int row)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height) return 0;
        return _data[CharBase + row * Width + col];
    }

    public void DrawBox(int col, int row, int w, int h, ConsoleColor? fg = null, ConsoleColor? bg = null)
    {
        if (w < 2 || h < 2) return;

        SetCell(col, row, '┌', fg, bg);
        SetCell(col + w - 1, row, '┐', fg, bg);
        SetCell(col, row + h - 1, '└', fg, bg);
        SetCell(col + w - 1, row + h - 1, '┘', fg, bg);

        for (int c = col + 1; c < col + w - 1; c++)
        {
            SetCell(c, row, '─', fg, bg);
            SetCell(c, row + h - 1, '─', fg, bg);
        }
        for (int r = row + 1; r < row + h - 1; r++)
        {
            SetCell(col, r, '│', fg, bg);
            SetCell(col + w - 1, r, '│', fg, bg);
        }
    }

    private void MarkDirty(int index)
    {
        if (!_dirty[index])
        {
            _dirty[index] = true;
            _dirtyCount++;
        }
    }

    private static ConsoleColor AttrToConsoleColor(byte attr)
    {
        return (attr & 0x0F) switch
        {
            0 => ConsoleColor.Black,
            1 => ConsoleColor.DarkBlue,
            2 => ConsoleColor.DarkGreen,
            3 => ConsoleColor.DarkCyan,
            4 => ConsoleColor.DarkRed,
            5 => ConsoleColor.DarkMagenta,
            6 => ConsoleColor.DarkYellow,
            7 => ConsoleColor.Gray,
            8 => ConsoleColor.DarkGray,
            9 => ConsoleColor.Blue,
            10 => ConsoleColor.Green,
            11 => ConsoleColor.Cyan,
            12 => ConsoleColor.Red,
            13 => ConsoleColor.Magenta,
            14 => ConsoleColor.Yellow,
            15 => ConsoleColor.White,
            _ => ConsoleColor.Gray
        };
    }

    private static byte ColorToAttr(ConsoleColor fg, ConsoleColor bg)
    {
        return (byte)(ColorToIndex(fg) | (ColorToIndex(bg) << 4));
    }

    private static int ColorToIndex(ConsoleColor c)
    {
        return c switch
        {
            ConsoleColor.Black => 0,
            ConsoleColor.DarkBlue => 1,
            ConsoleColor.DarkGreen => 2,
            ConsoleColor.DarkCyan => 3,
            ConsoleColor.DarkRed => 4,
            ConsoleColor.DarkMagenta => 5,
            ConsoleColor.DarkYellow => 6,
            ConsoleColor.Gray => 7,
            ConsoleColor.DarkGray => 8,
            ConsoleColor.Blue => 9,
            ConsoleColor.Green => 10,
            ConsoleColor.Cyan => 11,
            ConsoleColor.Red => 12,
            ConsoleColor.Magenta => 13,
            ConsoleColor.Yellow => 14,
            ConsoleColor.White => 15,
            _ => 7
        };
    }
}
