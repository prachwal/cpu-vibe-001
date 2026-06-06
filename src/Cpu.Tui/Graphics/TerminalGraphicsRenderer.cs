using Cpu.Tui.Rendering;

namespace Cpu.Tui.Graphics;

public enum TerminalGraphicsMode
{
    HalfBlockColor,
    BrailleMono,
    Grayscale,
    BestGlyph,
    BestGlyphTrueColor
}

public static class TerminalGraphicsRenderer
{
    private static readonly char[] Shades = [' ', '░', '▒', '▓', '█'];

    public static void Render(
        ITerminalRenderer renderer,
        PixelBuffer pixels,
        TerminalGraphicsMode mode,
        int x,
        int y,
        int cols,
        int rows)
    {
        switch (mode)
        {
            case TerminalGraphicsMode.BrailleMono:
                RenderBrailleMono(renderer, pixels.ResizeNearest(cols * 2, rows * 4), x, y, cols * 2, rows * 4);
                break;
            case TerminalGraphicsMode.Grayscale:
                RenderGrayscale(renderer, pixels.ResizeNearest(cols, rows), x, y, cols, rows);
                break;
            case TerminalGraphicsMode.BestGlyph:
                RenderBestGlyph(renderer, pixels.ResizeNearest(cols * 2, rows * 4), x, y, cols, rows);
                break;
            case TerminalGraphicsMode.BestGlyphTrueColor:
                RenderBestGlyphTrueColor(renderer, pixels.ResizeBilinear(cols * 2, rows * 4), x, y, cols, rows);
                break;
            default:
                RenderHalfBlock(renderer, pixels.ResizeNearest(cols, rows * 2), x, y, cols, rows * 2);
                break;
        }
    }

    public static void RenderHalfBlock(ITerminalRenderer renderer, PixelBuffer pixels, int x, int y, int width, int height)
    {
        int outputRows = (height + 1) / 2;
        for (int row = 0; row < outputRows; row++)
        {
            int topY = row * 2;
            int bottomY = topY + 1;
            for (int col = 0; col < width; col++)
            {
                Pixel top = pixels.GetPixel(col, topY);
                Pixel bottom = bottomY < height ? pixels.GetPixel(col, bottomY) : Pixel.Black;
                renderer.SetCell(x + col, y + row, '▀', ToConsoleColor(top), ToConsoleColor(bottom));
            }
        }
    }

    public static void RenderBrailleMono(
        ITerminalRenderer renderer,
        PixelBuffer pixels,
        int x,
        int y,
        int width,
        int height,
        byte threshold = 128)
    {
        int outputCols = (width + 1) / 2;
        int outputRows = (height + 3) / 4;

        for (int row = 0; row < outputRows; row++)
        {
            for (int col = 0; col < outputCols; col++)
            {
                int mask = 0;
                for (int py = 0; py < 4; py++)
                {
                    for (int px = 0; px < 2; px++)
                    {
                        int sourceX = col * 2 + px;
                        int sourceY = row * 4 + py;
                        if (sourceX >= width || sourceY >= height)
                            continue;

                        if (pixels.GetPixel(sourceX, sourceY).Luma >= threshold)
                            mask |= BrailleBit(px, py);
                    }
                }

                renderer.SetCell(x + col, y + row, (char)(0x2800 + mask), ConsoleColor.Gray, ConsoleColor.Black);
            }
        }
    }

    public static void RenderGrayscale(ITerminalRenderer renderer, PixelBuffer pixels, int x, int y, int width, int height)
    {
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                byte luma = pixels.GetPixel(col, row).Luma;
                TerminalColor gray = TerminalColor.FromRgb(luma, luma, luma);
                renderer.SetCell(x + col, y + row, '█', gray, TerminalColor.FromConsole(ConsoleColor.Black));
            }
        }
    }

    public static void RenderColorShade(ITerminalRenderer renderer, PixelBuffer pixels, int x, int y, int width, int height)
    {
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                Pixel pixel = pixels.GetPixel(col, row);
                int shadeIndex = pixel.Luma * (Shades.Length - 1) / 255;
                renderer.SetCell(x + col, y + row, Shades[shadeIndex], ToConsoleColor(pixel), ConsoleColor.Black);
            }
        }
    }

    private static readonly GlyphAtlas _bestGlyphAtlas = GlyphAtlas.CreateDefault2x4();
    private static readonly BestGlyphRenderer _bestGlyphRenderer = new(_bestGlyphAtlas);

    public static void RenderBestGlyph(ITerminalRenderer renderer, PixelBuffer pixels, int x, int y, int cols, int rows)
    {
        _bestGlyphRenderer.Render(renderer, pixels, x, y, cols, rows);
    }

    public static void RenderBestGlyphTrueColor(ITerminalRenderer renderer, PixelBuffer pixels, int x, int y, int cols, int rows)
    {
        _bestGlyphRenderer.RenderTrueColor(renderer, pixels, x, y, cols, rows);
    }

    private static int BrailleBit(int x, int y)
    {
        return (x, y) switch
        {
            (0, 0) => 0x01,
            (0, 1) => 0x02,
            (0, 2) => 0x04,
            (0, 3) => 0x40,
            (1, 0) => 0x08,
            (1, 1) => 0x10,
            (1, 2) => 0x20,
            (1, 3) => 0x80,
            _ => 0
        };
    }

    private static ConsoleColor ToConsoleColor(Pixel pixel)
    {
        ConsoleColor best = ConsoleColor.Black;
        int bestDistance = int.MaxValue;
        foreach ((ConsoleColor color, Pixel value) in Palette)
        {
            int dr = pixel.R - value.R;
            int dg = pixel.G - value.G;
            int db = pixel.B - value.B;
            int distance = dr * dr + dg * dg + db * db;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = color;
            }
        }

        return best;
    }

    private static readonly (ConsoleColor Color, Pixel Value)[] Palette =
    [
        (ConsoleColor.Black, new Pixel(0, 0, 0)),
        (ConsoleColor.DarkBlue, new Pixel(0, 0, 128)),
        (ConsoleColor.DarkGreen, new Pixel(0, 128, 0)),
        (ConsoleColor.DarkCyan, new Pixel(0, 128, 128)),
        (ConsoleColor.DarkRed, new Pixel(128, 0, 0)),
        (ConsoleColor.DarkMagenta, new Pixel(128, 0, 128)),
        (ConsoleColor.DarkYellow, new Pixel(128, 128, 0)),
        (ConsoleColor.Gray, new Pixel(192, 192, 192)),
        (ConsoleColor.DarkGray, new Pixel(128, 128, 128)),
        (ConsoleColor.Blue, new Pixel(0, 0, 255)),
        (ConsoleColor.Green, new Pixel(0, 255, 0)),
        (ConsoleColor.Cyan, new Pixel(0, 255, 255)),
        (ConsoleColor.Red, new Pixel(255, 0, 0)),
        (ConsoleColor.Magenta, new Pixel(255, 0, 255)),
        (ConsoleColor.Yellow, new Pixel(255, 255, 0)),
        (ConsoleColor.White, new Pixel(255, 255, 255))
    ];
}
