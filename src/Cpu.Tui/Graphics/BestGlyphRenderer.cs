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

                // Quick analysis: compute mean luma and contrast
                float meanLuma = 0;
                for (int i = 0; i < GlyphPattern.TileSize; i++)
                {
                    _tileLuma[i] = _tile[i].Luma;
                    meanLuma += _tileLuma[i];
                }
                meanLuma /= GlyphPattern.TileSize;

                // Compute variance (sum of abs differences from mean)
                float contrast = 0;
                for (int i = 0; i < GlyphPattern.TileSize; i++)
                    contrast += Math.Abs(_tileLuma[i] - meanLuma);
                contrast /= GlyphPattern.TileSize;

                char bestGlyph = ' ';
                ConsoleColor bestFg = ConsoleColor.Gray;
                ConsoleColor bestBg = ConsoleColor.Black;
                int bestScore = int.MaxValue;

                // Select glyph candidates based on tile contrast
                // Low contrast → basic glyphs only (space, █, shades)
                // High contrast → include braille
                int startIdx = 0;
                int endIdx = contrast > 40 ? _brailleEnd : _basicEnd;

                for (int gi = startIdx; gi < endIdx; gi++)
                {
                    GlyphPattern glyph = _atlas.Glyphs[gi];

                    Pixel fgRgb = PixelMath.EstimateForeground(_tile, glyph.Alpha, GlyphPattern.TileSize);
                    Pixel bgRgb = PixelMath.EstimateBackground(_tile, glyph.Alpha, GlyphPattern.TileSize);

                    ConsoleColor fg = ColorQuantizer.NearestConsoleColor(fgRgb.R, fgRgb.G, fgRgb.B);
                    ConsoleColor bg = ColorQuantizer.NearestConsoleColor(bgRgb.R, bgRgb.G, bgRgb.B);

                    // Fast path: if fg == bg, all glyphs produce same error, skip
                    if (fg == bg)
                    {
                        if (gi == 0) // space — set baseline
                        {
                            int s = PixelMath.ComputeTileErrorQuantized(_tile, glyph.Alpha, _colorRgb[(int)fg], _colorRgb[(int)bg]);
                            if (s < bestScore)
                            {
                                bestScore = s;
                                bestGlyph = glyph.Glyph;
                                bestFg = fg;
                                bestBg = bg;
                            }
                        }
                        continue; // all subsequent fg==bg glyphs produce same error
                    }

                    var (fgR, fgG, fgB) = _colorRgb[(int)fg];
                    var (bgR, bgG, bgB) = _colorRgb[(int)bg];
                    int score = PixelMath.ComputeTileErrorQuantized(_tile, glyph.Alpha, (fgR, fgG, fgB), (bgR, bgG, bgB));

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestGlyph = glyph.Glyph;
                        bestFg = fg;
                        bestBg = bg;
                        if (score == 0) goto NextCell; // perfect match, done
                    }
                }

                NextCell:
                renderer.SetCell(terminalX + col, terminalY + row, bestGlyph, bestFg, bestBg);
            }
        }
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
