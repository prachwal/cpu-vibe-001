using Cpu.Tui.Rendering;

namespace Cpu.Tui.Graphics;

/// <summary>
/// Renders images by choosing the best glyph for each terminal cell.
/// For each 2x4 pixel tile, evaluates glyphs in the atlas and picks
/// the one with minimum error against the original pixels.
/// 
/// Performance: uses tile analysis to pre-select glyph candidates,
/// early-exit on perfect match (score=0), and eliminates redundant
/// evaluations when foreground ≃ background.
/// </summary>
public sealed class BestGlyphRenderer
{
    private readonly GlyphAtlas _atlas;
    private readonly Pixel[] _tile = new Pixel[GlyphPattern.TileSize];
    private readonly byte[] _tileLuma = new byte[GlyphPattern.TileSize];

    // Glyph index ranges within atlas for quick filtering
    private readonly int _basicEnd;    // space, █, ░, ▒, ▓, ▀, ▄, ▌, ▐
    private readonly int _brailleStart;
    private readonly int _brailleEnd;

    // Pre-computed RGB values for ConsoleColor (index = (int)ConsoleColor)
    private static readonly (byte R, byte G, byte B)[] _colorRgb = new (byte, byte, byte)[16];

    static BestGlyphRenderer()
    {
        for (int i = 0; i < 16; i++)
        {
            var cc = (ConsoleColor)i;
            _colorRgb[i] = ColorQuantizer.ConsoleColorToRgb(cc);
        }
    }

    public BestGlyphRenderer(GlyphAtlas atlas)
    {
        _atlas = atlas;
        var glyphs = atlas.Glyphs;

        // Find boundaries in atlas:
        // [0..8] = basic glyphs (space, █, ░, ▒, ▓, ▀, ▄, ▌, ▐)
        _basicEnd = Math.Min(9, glyphs.Length);

        // The rest are braille (256 glyphs starting at index 9)
        _brailleStart = _basicEnd;
        _brailleEnd = glyphs.Length;
    }

    public void Render(
        ITerminalRenderer renderer,
        PixelBuffer image,
        int terminalX,
        int terminalY,
        int cols,
        int rows)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int px = col * GlyphPattern.TileWidth;
                int py = row * GlyphPattern.TileHeight;

                SampleTile(image, px, py);
                if (IsBlackTile())
                {
                    renderer.SetCell(terminalX + col, terminalY + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                    continue;
                }

                bool wireframe = TryGetWireframeNeutral(out Pixel neutralFg);
                float contrast = ComputeTileContrast();
                int endIdx = wireframe ? _basicEnd : contrast > 40 ? _brailleEnd : _basicEnd;

                char bestGlyph = ' ';
                ConsoleColor bestFg = ConsoleColor.Gray;
                ConsoleColor bestBg = ConsoleColor.Black;
                int bestScore = int.MaxValue;

                for (int gi = 0; gi < endIdx; gi++)
                {
                    GlyphPattern glyph = _atlas.Glyphs[gi];
                    Pixel fgRgb;
                    Pixel bgRgb;
                    if (wireframe)
                    {
                        fgRgb = neutralFg;
                        bgRgb = Pixel.Black;
                    }
                    else
                    {
                        fgRgb = PixelMath.EstimateForeground(_tile, glyph.Alpha, GlyphPattern.TileSize);
                        bgRgb = PixelMath.EstimateBackground(_tile, glyph.Alpha, GlyphPattern.TileSize);
                        if (IsDarkBackgroundTile(glyph.Alpha))
                            bgRgb = Pixel.Black;
                    }

                    ConsoleColor fg = ColorQuantizer.NearestConsoleColor(fgRgb.R, fgRgb.G, fgRgb.B);
                    ConsoleColor bg = ColorQuantizer.NearestConsoleColor(bgRgb.R, bgRgb.G, bgRgb.B);

                    if (fg == bg)
                    {
                        if (gi == 0)
                        {
                            int s = PixelMath.ComputeTileErrorWithLuma(_tile, glyph.Alpha,
                                _colorRgb[(int)fg], _colorRgb[(int)bg]);
                            if (s < bestScore)
                            {
                                bestScore = s;
                                bestGlyph = glyph.Glyph;
                                bestFg = fg;
                                bestBg = bg;
                            }
                        }
                        continue;
                    }

                    int score = PixelMath.ComputeTileErrorWithLuma(_tile, glyph.Alpha,
                        _colorRgb[(int)fg], _colorRgb[(int)bg]);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestGlyph = glyph.Glyph;
                        bestFg = fg;
                        bestBg = bg;
                        if (score == 0) goto NextCell;
                    }
                }

                NextCell:
                renderer.SetCell(terminalX + col, terminalY + row, bestGlyph, bestFg, bestBg);
            }
        }
    }

    /// <summary>
    /// Truecolor variant — no quantization to ConsoleColor.
    /// Computes fg/bg as raw RGB and uses TerminalColor.FromRgb.
    /// Error computed against truecolor pixel values.
    /// </summary>
    public void RenderTrueColor(
        ITerminalRenderer renderer,
        PixelBuffer image,
        int terminalX,
        int terminalY,
        int cols,
        int rows)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int px = col * GlyphPattern.TileWidth;
                int py = row * GlyphPattern.TileHeight;

                SampleTile(image, px, py);
                if (IsBlackTile())
                {
                    renderer.SetCell(terminalX + col, terminalY + row, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
                    continue;
                }

                bool wireframe = TryGetWireframeNeutral(out Pixel neutralFg);
                float contrast = ComputeTileContrast();
                int endIdx = wireframe ? _basicEnd : contrast > 40 ? _brailleEnd : _basicEnd;

                char bestGlyph = ' ';
                TerminalColor bestFg = TerminalColor.FromConsole(ConsoleColor.Gray);
                TerminalColor bestBg = TerminalColor.FromConsole(ConsoleColor.Black);
                int bestScore = int.MaxValue;

                for (int gi = 0; gi < endIdx; gi++)
                {
                    GlyphPattern glyph = _atlas.Glyphs[gi];
                    Pixel fgRgb;
                    Pixel bgRgb;
                    if (wireframe)
                    {
                        fgRgb = neutralFg;
                        bgRgb = Pixel.Black;
                    }
                    else
                    {
                        fgRgb = PixelMath.EstimateForeground(_tile, glyph.Alpha, GlyphPattern.TileSize);
                        bgRgb = PixelMath.EstimateBackground(_tile, glyph.Alpha, GlyphPattern.TileSize);
                        if (IsDarkBackgroundTile(glyph.Alpha))
                            bgRgb = Pixel.Black;
                    }

                    int score = PixelMath.ComputeTileErrorWithLuma(_tile, glyph.Alpha,
                        (fgRgb.R, fgRgb.G, fgRgb.B), (bgRgb.R, bgRgb.G, bgRgb.B));

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestGlyph = glyph.Glyph;
                        bestFg = ToTerminalColor(fgRgb);
                        bestBg = ToTerminalColor(bgRgb);
                        if (score == 0) goto NextCellTC;
                    }
                }

                NextCellTC:
                renderer.SetCell(terminalX + col, terminalY + row, bestGlyph, bestFg, bestBg);
            }
        }
    }

    private float ComputeTileContrast()
    {
        float meanLuma = 0;
        for (int i = 0; i < GlyphPattern.TileSize; i++)
        {
            _tileLuma[i] = _tile[i].Luma;
            meanLuma += _tileLuma[i];
        }
        meanLuma /= GlyphPattern.TileSize;

        float contrast = 0;
        for (int i = 0; i < GlyphPattern.TileSize; i++)
            contrast += Math.Abs(_tileLuma[i] - meanLuma);
        return contrast / GlyphPattern.TileSize;
    }

    private bool IsBlackTile()
    {
        for (int i = 0; i < GlyphPattern.TileSize; i++)
        {
            if (_tile[i].Luma > 18)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Monochrome sparse content (Demo3D wireframe): neutral gray fg, black bg, basic glyphs.
    /// </summary>
    private bool TryGetWireframeNeutral(out Pixel neutralFg)
    {
        neutralFg = Pixel.Black;
        int lumaSum = 0;
        int lit = 0;
        int dark = 0;
        for (int i = 0; i < GlyphPattern.TileSize; i++)
        {
            Pixel p = _tile[i];
            if (p.Luma <= 18)
            {
                dark++;
                continue;
            }
            if (p.R != p.G || p.G != p.B)
            {
                int drift = Math.Max(Math.Abs(p.R - p.G), Math.Abs(p.G - p.B));
                if (drift > 8)
                    return false;
            }
            lit++;
            lumaSum += p.Luma;
        }

        if (lit == 0 || dark < 2)
            return false;

        byte level = (byte)(lumaSum / lit);
        if (level >= 230)
            return false;

        neutralFg = new Pixel(level, level, level);
        return true;
    }

    private bool IsDarkBackgroundTile(byte[] alpha)
    {
        int backgroundCount = 0;
        int darkBackgroundCount = 0;
        for (int i = 0; i < GlyphPattern.TileSize; i++)
        {
            if (alpha[i] > 128) continue;
            backgroundCount++;
            if (_tile[i].Luma <= 18)
                darkBackgroundCount++;
        }

        return backgroundCount > 0 && darkBackgroundCount * 4 >= backgroundCount * 3;
    }

    private static TerminalColor ToTerminalColor(Pixel pixel)
    {
        return pixel.R <= 2 && pixel.G <= 2 && pixel.B <= 2
            ? TerminalColor.FromConsole(ConsoleColor.Black)
            : TerminalColor.FromRgb(pixel.R, pixel.G, pixel.B);
    }

    private void SampleTile(PixelBuffer image, int px, int py)
    {
        for (int y = 0; y < GlyphPattern.TileHeight; y++)
        {
            for (int x = 0; x < GlyphPattern.TileWidth; x++)
            {
                int srcX = Math.Min(px + x, image.Width - 1);
                int srcY = Math.Min(py + y, image.Height - 1);
                _tile[y * GlyphPattern.TileWidth + x] = image.GetPixel(srcX, srcY);
            }
        }
    }
}
