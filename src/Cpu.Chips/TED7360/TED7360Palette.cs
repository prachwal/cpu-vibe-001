namespace Cpu.Chips.TED7360;

public static class TED7360Palette
{
    private static readonly (byte R, byte G, byte B)[] ChromaColors =
    [
        (0x00, 0x00, 0x00), // 0 black
        (0xFF, 0xFF, 0xFF), // 1 white
        (0xA0, 0x20, 0x20), // 2 red
        (0x50, 0xC0, 0xE0), // 3 cyan
        (0xC0, 0x40, 0xC0), // 4 purple
        (0x30, 0xC0, 0x30), // 5 green
        (0x20, 0x20, 0xA0), // 6 blue
        (0xE0, 0xE0, 0x40), // 7 yellow
        (0xC0, 0x60, 0x20), // 8 orange
        (0x60, 0x30, 0x10), // 9 brown
        (0xE0, 0x80, 0x80), // 10 light red
        (0x40, 0x40, 0x40), // 11 dark grey
        (0x80, 0x80, 0x80), // 12 grey
        (0x80, 0xE0, 0x80), // 13 light green
        (0x80, 0x80, 0xE0), // 14 light blue
        (0xE0, 0x80, 0xE0), // 15 light purple
    ];

    public static (byte R, byte G, byte B) ToRgb(byte chromaLuma)
    {
        int chroma = chromaLuma & 0x0F;
        int luma = (chromaLuma >> 4) & 0x07;
        return ScaleLuma(ChromaColors[chroma], luma);
    }

    public static byte Pack(int luma, int chroma) =>
        (byte)(((luma & 7) << 4) | (chroma & 0x0F));

    public static byte PackFromComponents(int luma, int chroma) => Pack(luma, chroma);

    private static (byte R, byte G, byte B) ScaleLuma((byte R, byte G, byte B) c, int luma)
    {
        if (c.R == 0 && c.G == 0 && c.B == 0)
            return (0, 0, 0);

        int max = 7;
        int level = luma;
        return (
            (byte)(c.R * level / max),
            (byte)(c.G * level / max),
            (byte)(c.B * level / max));
    }
}
