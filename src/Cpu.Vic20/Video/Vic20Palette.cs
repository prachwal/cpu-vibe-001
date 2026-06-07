namespace Cpu.Vic20.Video;

public static class Vic20Palette
{
    public static readonly (byte R, byte G, byte B)[] Rgb =
    [
        (0, 0, 0),
        (255, 255, 255),
        (136, 0, 0),
        (170, 255, 238),
        (204, 68, 204),
        (0, 204, 85),
        (0, 0, 170),
        (238, 238, 119),
        (221, 136, 85),
        (170, 102, 68),
        (238, 119, 119),
        (170, 255, 255),
        (187, 136, 255),
        (170, 255, 170),
        (119, 119, 255),
        (255, 255, 170)
    ];

    public static (byte R, byte G, byte B) ToRgb(byte index) => Rgb[index & 0x0F];
}
