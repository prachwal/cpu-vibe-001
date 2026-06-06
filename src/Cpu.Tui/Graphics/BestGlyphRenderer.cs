using Cpu.Tui.Rendering;

namespace Cpu.Tui.Graphics;

/// <summary>
/// Renders images by choosing the best glyph for each terminal cell.
/// For each 2x4 pixel tile, evaluates all glyphs in the atlas and picks
/// the one with minimum error against the original pixels.
/// </summary>
public sealed class BestGlyphRenderer
{
    private readonly GlyphAtlas _atlas;
    private readonly byte[] _fgWeights = new byte[GlyphPattern.TileSize];
    private readonly byte[] _bgWeights = new byte[GlyphPattern.TileSize];
    private readonly Pixel[] _tile = new Pixel[GlyphPattern.TileSize];

    public BestGlyphRenderer(GlyphAtlas atlas)
    {
        _atlas = atlas;
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

                char bestGlyph = ' ';
                ConsoleColor bestFg = ConsoleColor.Gray;
                ConsoleColor bestBg = ConsoleColor.Black;
                int bestScore = int.MaxValue;

                foreach (GlyphPattern glyph in _atlas.Glyphs)
                {
                    Pixel fgRgb = PixelMath.EstimateForeground(_tile, glyph.Alpha, GlyphPattern.TileSize);
                    Pixel bgRgb = PixelMath.EstimateBackground(_tile, glyph.Alpha, GlyphPattern.TileSize);

                    ConsoleColor fg = ColorQuantizer.NearestConsoleColor(fgRgb.R, fgRgb.G, fgRgb.B);
                    ConsoleColor bg = ColorQuantizer.NearestConsoleColor(bgRgb.R, bgRgb.G, bgRgb.B);

                    (byte fgR, byte fgG, byte fgB) = ColorQuantizer.ConsoleColorToRgb(fg);
                    (byte bgR, byte bgG, byte bgB) = ColorQuantizer.ConsoleColorToRgb(bg);
                    Pixel fgQ = new(fgR, fgG, fgB);
                    Pixel bgQ = new(bgR, bgG, bgB);

                    int score = PixelMath.ComputeTileError(_tile, glyph.Alpha, fgQ, bgQ);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestGlyph = glyph.Glyph;
                        bestFg = fg;
                        bestBg = bg;
                    }
                }

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
