using Cpu.Chips.Vic20;
using Cpu.Chips.Vic6560;
using Cpu.Tui.Graphics;

namespace Cpu.Vic20.Video;

public sealed class Vic20Video
{
    public const int MaxWidth = 176;
    public const int MaxHeight = 184;
    public const int CharWidth = 8;
    public const int MaxCharsPerRow = 22;
    public const int MaxCharRows = 23;

    private readonly Vic6560Chip _vic;
    private readonly Func<ushort, byte> _readMemory;
    private readonly Func<ushort, byte> _readCharRom;
    private readonly Func<ushort, byte> _readColorRam;
    private readonly byte[] _pixels = new byte[MaxWidth * MaxHeight];

    public Vic20Video(
        Vic6560Chip vic,
        Func<ushort, byte> readMemory,
        Func<ushort, byte> readCharRom,
        Func<ushort, byte> readColorRam)
    {
        _vic = vic;
        _readMemory = readMemory;
        _readCharRom = readCharRom;
        _readColorRam = readColorRam;
    }

    public ReadOnlySpan<byte> Pixels => _pixels;

    public void RenderFrame()
    {
        Array.Clear(_pixels, 0, _pixels.Length);

        int cols = _vic.Columns;
        int rows = _vic.Rows;
        if (cols == 0)
            cols = MaxCharsPerRow;
        if (rows == 0)
            rows = MaxCharRows;

        int screenAddr = _vic.ScreenAddr;
        byte screenColor = _vic.ScreenColor;
        byte auxColor = _vic.AuxColor;
        bool reverse = _vic.ReverseMode;
        bool doubleHeight = _vic.DoubleHeightChars;
        int charHeight = doubleHeight ? 16 : 8;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                byte screenCode = _readMemory((ushort)(screenAddr + row * cols + col));
                bool charReverse = (screenCode & 0x80) != 0;
                int charIndex = screenCode & 0x7F;
                byte colorIndex = _readColorRam((ushort)(Vic20MemoryMap.ColorRamStart + col + row * cols));
                byte ink = colorIndex;
                byte paper = screenColor;
                if (reverse || charReverse)
                    (ink, paper) = (paper, ink);
                if (ink == 8)
                    ink = auxColor;

                RenderChar(col * CharWidth, row * charHeight, charIndex, _vic.CharAddr,
                    charHeight, ink, paper, colorIndex, screenColor, auxColor);
            }
        }
    }

    public PixelBuffer ToPixelBuffer()
    {
        var buffer = new PixelBuffer(MaxWidth, MaxHeight);
        for (int y = 0; y < MaxHeight; y++)
        {
            for (int x = 0; x < MaxWidth; x++)
            {
                (byte r, byte g, byte b) = Vic20Palette.ToRgb(_pixels[y * MaxWidth + x]);
                buffer.SetPixel(x, y, new Pixel(r, g, b));
            }
        }
        return buffer;
    }

    private void RenderChar(
        int px,
        int py,
        int charIndex,
        int charAddr,
        int charHeight,
        byte ink,
        byte paper,
        byte colorIndex,
        byte screenColor,
        byte auxColor)
    {
        bool multicolor = (colorIndex & 0x08) != 0;

        for (int y = 0; y < charHeight; y++)
        {
            byte glyphRow = _readCharRom((ushort)(charAddr + charIndex * 8 + (y % 8)));
            for (int x = 0; x < CharWidth; x++)
            {
                int pixelX = px + x;
                int pixelY = py + y;
                if (pixelX >= MaxWidth || pixelY >= MaxHeight)
                    continue;

                byte color;
                if (multicolor)
                {
                    int pair = (glyphRow >> (6 - (x / 2))) & 0x03;
                    color = pair switch
                    {
                        0 => screenColor,
                        1 => auxColor,
                        2 => paper,
                        _ => ink
                    };
                }
                else
                {
                    bool on = (glyphRow & (1 << (7 - x))) != 0;
                    color = on ? ink : paper;
                }

                _pixels[pixelY * MaxWidth + pixelX] = color;
            }
        }
    }
}
