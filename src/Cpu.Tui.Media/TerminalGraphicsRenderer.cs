using Cpu.Tui.Diagnostics;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Graphics;

public static class TerminalGraphicsRenderer
{
    private static readonly char[] Shades = [' ', '░', '▒', '▓', '█'];

    public static PixelBuffer ScaleForCells(PixelBuffer source, int cols, int rows, TerminalGraphicsMode mode)
    {
        (int targetWidth, int targetHeight) = TerminalGraphicsModes.TargetPixelSize(cols, rows, mode);
        if (source.Width == targetWidth && source.Height == targetHeight)
            return source;
        return TerminalGraphicsModes.PrefersBilinearResize(mode)
            ? source.ResizeBilinear(targetWidth, targetHeight)
            : source.ResizeNearest(targetWidth, targetHeight);
    }

    public static void Render(
        ITerminalRenderer renderer,
        PixelBuffer pixels,
        TerminalGraphicsMode mode,
        int x,
        int y,
        int cols,
        int rows)
    {
        PixelBuffer scaled = ScaleForCells(pixels, cols, rows, mode);
        RenderScaled(renderer, scaled, mode, x, y, cols, rows);
    }

    public static void RenderScaled(
        ITerminalRenderer renderer,
        PixelBuffer pixels,
        TerminalGraphicsMode mode,
        int x,
        int y,
        int cols,
        int rows)
    {
        RenderLog.Event("TerminalGraphicsRenderer.RenderScaled",
            $"mode={mode} target=({x},{y},{cols},{rows}) source={pixels.Width}x{pixels.Height}");
        switch (mode)
        {
            case TerminalGraphicsMode.BrailleMono:
                RenderBrailleMono(renderer, pixels, x, y, pixels.Width, pixels.Height);
                break;
            case TerminalGraphicsMode.Grayscale:
                RenderGrayscale(renderer, pixels, x, y, pixels.Width, pixels.Height);
                break;
            case TerminalGraphicsMode.ColorShade:
                RenderColorShade(renderer, pixels, x, y, pixels.Width, pixels.Height);
                break;
            case TerminalGraphicsMode.TrueTone:
                RenderTrueTone(renderer, pixels, x, y, pixels.Width, pixels.Height);
                break;
            case TerminalGraphicsMode.BestGlyph:
                RenderBestGlyph(renderer, pixels, x, y, cols, rows);
                break;
            case TerminalGraphicsMode.BestGlyphTrueColor:
                RenderBestGlyphTrueColor(renderer, pixels, x, y, cols, rows);
                break;
            default:
                RenderHalfBlock(renderer, pixels, x, y, pixels.Width, pixels.Height);
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
                if (IsBlack(top) && IsBlack(bottom))
                {
                    renderer.SetCell(x + col, y + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                    continue;
                }
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

                if (mask == 0)
                    renderer.SetCell(x + col, y + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                else
                    renderer.SetCell(x + col, y + row, (char)(0x2800 + mask), ConsoleColor.Gray, ConsoleColor.Black);
            }
        }
    }

    public static void RenderTrueTone(ITerminalRenderer renderer, PixelBuffer pixels, int x, int y, int width, int height)
    {
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                Pixel pixel = pixels.GetPixel(col, row);
                if (IsBlack(pixel))
                {
                    renderer.SetCell(x + col, y + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                    continue;
                }
                TerminalColor color = TerminalColor.FromRgb(pixel.R, pixel.G, pixel.B);
                renderer.SetCell(x + col, y + row, ' ', color, color);
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
                if (luma <= 2)
                {
                    renderer.SetCell(x + col, y + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                    continue;
                }
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
                if (IsBlack(pixel))
                {
                    renderer.SetCell(x + col, y + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                    continue;
                }
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

    private static ConsoleColor ToConsoleColor(Pixel pixel) =>
        TerminalPalette16.Nearest(pixel.R, pixel.G, pixel.B);

    private static bool IsBlack(Pixel pixel) => pixel.R <= 2 && pixel.G <= 2 && pixel.B <= 2;
}
