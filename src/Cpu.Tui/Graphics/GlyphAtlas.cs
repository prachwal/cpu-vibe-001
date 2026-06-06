namespace Cpu.Tui.Graphics;

/// <summary>
/// Atlas of glyph patterns for 2x4 pixel tiles.
/// Each glyph has an alpha mask showing which pixels are foreground.
/// </summary>
public sealed class GlyphAtlas
{
    public GlyphPattern[] Glyphs { get; }
    public int TileWidth => GlyphPattern.TileWidth;
    public int TileHeight => GlyphPattern.TileHeight;

    private GlyphAtlas(GlyphPattern[] glyphs)
    {
        Glyphs = glyphs;
    }

    /// <summary>
    /// Create default atlas with space, full block, shades, half-block, and braille.
    /// </summary>
    public static GlyphAtlas CreateDefault2x4()
    {
        var glyphs = new List<GlyphPattern>();

        // Space (empty)
        glyphs.Add(GlyphPattern.Empty(' '));

        // Full block
        glyphs.Add(GlyphPattern.Full('█'));

        // Shades (approximate alpha levels)
        glyphs.Add(new GlyphPattern('░', [64, 64, 64, 64, 64, 64, 64, 64]));
        glyphs.Add(new GlyphPattern('▒', [128, 128, 128, 128, 128, 128, 128, 128]));
        glyphs.Add(new GlyphPattern('▓', [192, 192, 192, 192, 192, 192, 192, 192]));

        // Half-block: ▀ (top half = fg, bottom half = bg)
        glyphs.Add(new GlyphPattern('▀', [255, 255, 255, 255, 0, 0, 0, 0]));

        // Half-block: ▄ (top half = bg, bottom half = fg)
        glyphs.Add(new GlyphPattern('▄', [0, 0, 0, 0, 255, 255, 255, 255]));

        // Block elements
        glyphs.Add(new GlyphPattern('▌', [255, 0, 255, 0, 255, 0, 255, 0]));
        glyphs.Add(new GlyphPattern('▐', [0, 255, 0, 255, 0, 255, 0, 255]));

        // Braille patterns (U+2800..U+28FF)
        // Braille cell layout (2x4):
        //   dot1 dot4
        //   dot2 dot5
        //   dot3 dot6
        //   dot7 dot8
        for (int i = 0; i < 256; i++)
        {
            char braille = (char)(0x2800 + i);
            byte[] alpha = BrailleToAlpha(i);
            glyphs.Add(new GlyphPattern(braille, alpha));
        }

        return new GlyphAtlas(glyphs.ToArray());
    }

    /// <summary>
    /// Convert braille bitmask to 2x4 alpha array.
    /// Braille encoding: bit0=dot1, bit1=dot2, bit2=dot3, bit3=dot4,
    ///                   bit4=dot5, bit5=dot6, bit6=dot7, bit7=dot8
    /// </summary>
    private static byte[] BrailleToAlpha(int bits)
    {
        // Braille dot positions in 2x4 grid:
        // dot1(0,0) dot4(1,0)
        // dot2(0,1) dot5(1,1)
        // dot3(0,2) dot6(1,2)
        // dot7(0,3) dot8(1,3)
        int[] bitToIndex = [0, 2, 4, 1, 3, 5, 6, 7];
        byte[] alpha = new byte[8];
        for (int b = 0; b < 8; b++)
        {
            if ((bits & (1 << b)) != 0)
                alpha[bitToIndex[b]] = 255;
        }
        return alpha;
    }
}
