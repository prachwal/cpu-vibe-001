namespace Cpu.Tui.Graphics;

/// <summary>
/// Maps RGB colors to ConsoleColor (16-color palette).
/// </summary>
public static class ColorQuantizer
{
    private static readonly (ConsoleColor Color, byte R, byte G, byte B)[] Palette =
    [
        (ConsoleColor.Black, 0, 0, 0),
        (ConsoleColor.DarkBlue, 0, 0, 128),
        (ConsoleColor.DarkGreen, 0, 128, 0),
        (ConsoleColor.DarkCyan, 0, 128, 128),
        (ConsoleColor.DarkRed, 128, 0, 0),
        (ConsoleColor.DarkMagenta, 128, 0, 128),
        (ConsoleColor.DarkYellow, 128, 128, 0),
        (ConsoleColor.Gray, 192, 192, 192),
        (ConsoleColor.DarkGray, 128, 128, 128),
        (ConsoleColor.Blue, 0, 0, 255),
        (ConsoleColor.Green, 0, 255, 0),
        (ConsoleColor.Cyan, 0, 255, 255),
        (ConsoleColor.Red, 255, 0, 0),
        (ConsoleColor.Magenta, 255, 0, 255),
        (ConsoleColor.Yellow, 255, 255, 0),
        (ConsoleColor.White, 255, 255, 255)
    ];

    public static ConsoleColor NearestConsoleColor(byte r, byte g, byte b)
    {
        ConsoleColor best = ConsoleColor.Black;
        int bestDist = int.MaxValue;

        foreach (var (color, pr, pg, pb) in Palette)
        {
            int dr = r - pr;
            int dg = g - pg;
            int db = b - pb;
            int dist = dr * dr + dg * dg + db * db;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = color;
            }
        }

        return best;
    }

    public static (byte R, byte G, byte B) ConsoleColorToRgb(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => (0, 0, 0),
        ConsoleColor.DarkBlue => (0, 0, 128),
        ConsoleColor.DarkGreen => (0, 128, 0),
        ConsoleColor.DarkCyan => (0, 128, 128),
        ConsoleColor.DarkRed => (128, 0, 0),
        ConsoleColor.DarkMagenta => (128, 0, 128),
        ConsoleColor.DarkYellow => (128, 128, 0),
        ConsoleColor.Gray => (192, 192, 192),
        ConsoleColor.DarkGray => (128, 128, 128),
        ConsoleColor.Blue => (0, 0, 255),
        ConsoleColor.Green => (0, 255, 0),
        ConsoleColor.Cyan => (0, 255, 255),
        ConsoleColor.Red => (255, 0, 0),
        ConsoleColor.Magenta => (255, 0, 255),
        ConsoleColor.Yellow => (255, 255, 0),
        ConsoleColor.White => (255, 255, 255),
        _ => (192, 192, 192)
    };
}
