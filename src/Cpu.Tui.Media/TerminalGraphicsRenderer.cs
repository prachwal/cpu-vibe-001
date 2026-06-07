using Cpu.Tui.Diagnostics;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Graphics;

public static class TerminalGraphicsRenderer
{
    private static readonly char[] Shades = [' ', '░', '▒', '▓', '█'];
    private static readonly float[] ShadeDensities = [0f, 0.25f, 0.5f, 0.75f, 1f];

    public static PixelBuffer ScaleForCells(PixelBuffer source, int cols, int rows, TerminalGraphicsMode mode)
    {
        (int layoutWidth, int layoutHeight) = TerminalGraphicsModes.FrameTargetPixelSize(cols, rows);
        (int targetWidth, int targetHeight) = TerminalGraphicsModes.TargetPixelSize(cols, rows, mode);

        double contentScale = Math.Min((double)layoutWidth / source.Width, (double)layoutHeight / source.Height);
        int scaledWidth = Math.Max(1, (int)Math.Round(source.Width * contentScale));
        int scaledHeight = Math.Max(1, (int)Math.Round(source.Height * contentScale));

        PixelBuffer scaled = PrefersBilinearResize(mode)
            ? source.ResizeBilinear(scaledWidth, scaledHeight)
            : source.ResizeNearest(scaledWidth, scaledHeight);

        PixelBuffer layout = scaledWidth == layoutWidth && scaledHeight == layoutHeight
            ? scaled
            : scaled.Letterbox(layoutWidth, layoutHeight);

        return mode switch
        {
            TerminalGraphicsMode.Grayscale or TerminalGraphicsMode.ColorShade or TerminalGraphicsMode.TrueTone
                => MergeVerticalPairs(layout, cols, rows),
            TerminalGraphicsMode.BrailleMono or TerminalGraphicsMode.BestGlyph or TerminalGraphicsMode.BestGlyphTrueColor
                => layout.ResizeNearest(targetWidth, targetHeight),
            _ => layout
        };
    }

    private static PixelBuffer MergeVerticalPairs(PixelBuffer layout, int cols, int rows)
    {
        PixelBuffer merged = new(cols, rows);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Pixel top = layout.GetPixel(x, y * 2);
                Pixel bottom = layout.GetPixel(x, y * 2 + 1);
                merged.SetPixel(x, y, AveragePixels(top, bottom));
            }
        }

        return merged;
    }

    private static Pixel AveragePixels(Pixel a, Pixel b) => new(
        (byte)((a.R + b.R) / 2),
        (byte)((a.G + b.G) / 2),
        (byte)((a.B + b.B) / 2));

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
        int height)
    {
        int outputCols = (width + 1) / 2;
        int outputRows = (height + 3) / 4;

        for (int row = 0; row < outputRows; row++)
        {
            for (int col = 0; col < outputCols; col++)
            {
                int mask = 0;
                int lumaSum = 0;
                int pixelCount = 0;
                int litLumaSum = 0;
                int litCount = 0;

                for (int py = 0; py < 4; py++)
                {
                    for (int px = 0; px < 2; px++)
                    {
                        int sourceX = col * 2 + px;
                        int sourceY = row * 4 + py;
                        if (sourceX >= width || sourceY >= height)
                            continue;

                        byte luma = pixels.GetPixel(sourceX, sourceY).Luma;
                        lumaSum += luma;
                        pixelCount++;
                    }
                }

                byte threshold = pixelCount > 0
                    ? (byte)Math.Clamp(lumaSum / pixelCount, 24, 232)
                    : (byte)128;

                for (int py = 0; py < 4; py++)
                {
                    for (int px = 0; px < 2; px++)
                    {
                        int sourceX = col * 2 + px;
                        int sourceY = row * 4 + py;
                        if (sourceX >= width || sourceY >= height)
                            continue;

                        byte luma = pixels.GetPixel(sourceX, sourceY).Luma;
                        if (luma >= threshold)
                        {
                            mask |= BrailleBit(px, py);
                            litLumaSum += luma;
                            litCount++;
                        }
                    }
                }

                if (mask == 0)
                {
                    renderer.SetCell(x + col, y + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                    continue;
                }

                byte fgLevel = (byte)(litCount > 0 ? litLumaSum / litCount : threshold);
                ConsoleColor fg = MonoGrayForLuma(fgLevel);
                renderer.SetCell(x + col, y + row, (char)(0x2800 + mask), fg, ConsoleColor.Black);
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

                (char shade, ConsoleColor fg) = PickBestColorShade(pixel);
                renderer.SetCell(x + col, y + row, shade, fg, ConsoleColor.Black);
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

    private static (char Shade, ConsoleColor Fg) PickBestColorShade(Pixel pixel)
    {
        int bestDist = int.MaxValue;
        char bestShade = Shades[^1];
        ConsoleColor bestFg = ConsoleColor.White;

        foreach ((ConsoleColor color, byte pr, byte pg, byte pb) in TerminalPalette16.Entries)
        {
            if (color == ConsoleColor.Black)
                continue;

            for (int shadeIndex = 1; shadeIndex < Shades.Length; shadeIndex++)
            {
                float density = ShadeDensities[shadeIndex];
                int er = (int)(pr * density);
                int eg = (int)(pg * density);
                int eb = (int)(pb * density);
                int dr = pixel.R - er;
                int dg = pixel.G - eg;
                int db = pixel.B - eb;
                int dist = dr * dr + dg * dg + db * db;
                if (dist >= bestDist)
                    continue;

                bestDist = dist;
                bestShade = Shades[shadeIndex];
                bestFg = color;
            }
        }

        return (bestShade, bestFg);
    }

    private static ConsoleColor MonoGrayForLuma(byte luma) => luma switch
    {
        < 48 => ConsoleColor.DarkGray,
        < 160 => ConsoleColor.Gray,
        _ => ConsoleColor.White
    };

    private static bool PrefersBilinearResize(TerminalGraphicsMode mode) =>
        mode is TerminalGraphicsMode.TrueTone or TerminalGraphicsMode.ColorShade;
    private static ConsoleColor ToConsoleColor(Pixel pixel) =>
        TerminalPalette16.Nearest(pixel.R, pixel.G, pixel.B);

    private static bool IsBlack(Pixel pixel) => pixel.R <= 2 && pixel.G <= 2 && pixel.B <= 2;
}
