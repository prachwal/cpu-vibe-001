namespace Cpu.Tui.Graphics;

/// <summary>
/// ZX Spectrum color palette (8 standard + 8 bright = 16 colors).
/// </summary>
public static class SpectrumPalette
{
    // ZX Spectrum 8 standard colors (index 0-7)
    private static readonly Pixel[] Standard =
    [
        new(0, 0, 0),       // 0 Black
        new(0, 0, 215),     // 1 Blue
        new(215, 0, 0),     // 2 Red
        new(215, 0, 215),   // 3 Magenta
        new(0, 215, 0),     // 4 Green
        new(0, 215, 215),   // 5 Cyan
        new(215, 215, 0),   // 6 Yellow
        new(215, 215, 215), // 7 White
    ];

    // Bright variants (index 8-15)
    private static readonly Pixel[] Bright =
    [
        new(0, 0, 0),       // 8 Black (same)
        new(0, 0, 255),     // 9 Bright Blue
        new(255, 0, 0),     // 10 Bright Red
        new(255, 0, 255),   // 11 Bright Magenta
        new(0, 255, 0),     // 12 Bright Green
        new(0, 255, 255),   // 13 Bright Cyan
        new(255, 255, 0),   // 14 Bright Yellow
        new(255, 255, 255), // 15 Bright White
    ];

    /// <summary>
    /// Get RGB for a Spectrum color index (0-15).
    /// Bits: b0-2 = color, b3 = bright.
    /// </summary>
    public static Pixel GetColor(int index)
    {
        index &= 0x0F;
        return (index & 8) != 0 ? Bright[index & 7] : Standard[index];
    }
}

/// <summary>
/// ZX Spectrum-like canvas: pixel bitmap + 32×24 attribute grid.
/// Each 8×8 cell has one INK (foreground) and PAPER (background) color.
/// </summary>
public sealed class AttributeCanvas
{
    public const int PixelWidth = 256;
    public const int PixelHeight = 192;
    public const int AttrCols = 32;
    public const int AttrRows = 24;
    public const int BlockSize = 8;

    private readonly byte[] _pixels = new byte[PixelWidth * PixelHeight]; // 0 or 1
    private readonly byte[] _attrs = new byte[AttrCols * AttrRows];      // lo=INK, hi=PAPER

    public int Width => PixelWidth;
    public int Height => PixelHeight;

    /// <summary>
    /// Set a pixel: 1 = INK (foreground), 0 = PAPER (background).
    /// </summary>
    public void SetPixel(int x, int y, byte ink)
    {
        if ((uint)x < PixelWidth && (uint)y < PixelHeight)
            _pixels[y * PixelWidth + x] = (byte)(ink & 1);
    }

    public byte GetPixel(int x, int y)
    {
        if ((uint)x < PixelWidth && (uint)y < PixelHeight)
            return _pixels[y * PixelWidth + x];
        return 0;
    }

    /// <summary>
    /// Set attribute for block (col, row). attr = ink | (paper << 3) | (bright << 6).
    /// </summary>
    public void SetAttr(int col, int row, int ink, int paper, bool bright = false)
    {
        if ((uint)col < AttrCols && (uint)row < AttrRows)
            _attrs[row * AttrCols + col] = (byte)(ink | (paper << 3) | (bright ? 0x40 : 0));
    }

    /// <summary>
    /// Fill an area with pixel data.
    /// </summary>
    public void FillBlock(int ax, int ay, byte value)
    {
        for (int y = 0; y < BlockSize; y++)
            for (int x = 0; x < BlockSize; x++)
                SetPixel(ax * BlockSize + x, ay * BlockSize + y, value);
    }

    /// <summary>
    /// Render to a PixelBuffer using current attributes.
    /// </summary>
    public void RenderTo(PixelBuffer target)
    {
        for (int ay = 0; ay < AttrRows && ay * BlockSize < target.Height; ay++)
        {
            for (int ax = 0; ax < AttrCols && ax * BlockSize < target.Width; ax++)
            {
                int attr = _attrs[ay * AttrCols + ax];
                int ink = attr & 7;
                int paper = (attr >> 3) & 7;
                bool bright = (attr & 0x40) != 0;
                Pixel inkColor = bright ? SpectrumPalette.GetColor(ink | 8) : SpectrumPalette.GetColor(ink);
                Pixel paperColor = bright ? SpectrumPalette.GetColor(paper | 8) : SpectrumPalette.GetColor(paper);

                for (int y = 0; y < BlockSize && ay * BlockSize + y < target.Height; y++)
                {
                    for (int x = 0; x < BlockSize && ax * BlockSize + x < target.Width; x++)
                    {
                        int px = ax * BlockSize + x;
                        int py = ay * BlockSize + y;
                        bool isInk = (_pixels[py * PixelWidth + px] & 1) != 0;
                        target.SetPixel(px, py, isInk ? inkColor : paperColor);
                    }
                }
            }
        }
    }
}
