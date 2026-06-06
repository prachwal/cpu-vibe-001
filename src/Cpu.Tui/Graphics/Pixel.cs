namespace Cpu.Tui.Graphics;

public readonly record struct Pixel(byte R, byte G, byte B)
{
    public byte Luma => (byte)((R * 30 + G * 59 + B * 11) / 100);

    public static readonly Pixel Black = new(0, 0, 0);
    public static readonly Pixel White = new(255, 255, 255);
}
