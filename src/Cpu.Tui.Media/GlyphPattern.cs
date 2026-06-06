namespace Cpu.Tui.Graphics;

/// <summary>
/// A glyph with its alpha mask for a 2x4 pixel tile.
/// Alpha=0 means background, Alpha=255 means foreground.
/// </summary>
public readonly record struct GlyphPattern(char Glyph, byte[] Alpha)
{
    public const int TileWidth = 2;
    public const int TileHeight = 4;
    public const int TileSize = TileWidth * TileHeight;

    public static GlyphPattern FromBinary(char glyph, int bits)
    {
        byte[] alpha = new byte[TileSize];
        for (int i = 0; i < TileSize; i++)
            alpha[i] = ((bits >> i) & 1) == 1 ? (byte)255 : (byte)0;
        return new GlyphPattern(glyph, alpha);
    }

    public static GlyphPattern Full(char glyph) => new(glyph, [255, 255, 255, 255, 255, 255, 255, 255]);
    public static GlyphPattern Empty(char glyph) => new(glyph, [0, 0, 0, 0, 0, 0, 0, 0]);
}
