using Cpu.Tui.Graphics;

namespace Cpu.Tui.Rendering;

public sealed class PresentationSession
{
    private readonly ITerminalRenderer _renderer;
    private TermRect _area;

    public int Width => _area.W;
    public int Height => _area.H;
    public TermRect Area => _area;

    public PresentationSession(ITerminalRenderer renderer, TermRect area)
    {
        _renderer = renderer;
        _area = area;
    }

    public void SetArea(TermRect area) => _area = area;

    public void Clear(string source = "")
    {
        TermArea.Clear(_renderer, _area, TerminalCell.Black, source);
    }

    public void Write(int x, int y, string text, ConsoleColor fg, ConsoleColor bg)
    {
        if (y < 0 || y >= _area.H || x >= _area.W) return;
        int maxLen = _area.W - x;
        if (text.Length > maxLen) text = text[..maxLen];
        if (text.Length > 0)
            TermArea.Write(_renderer, _area, x, y, text, fg, bg);
    }

    public void Centered(string[] lines, ConsoleColor fg, ConsoleColor bg)
    {
        TermArea.Centered(_renderer, _area, lines, fg, bg);
    }

    public TermRect DrawFrame(TermRect rect, FrameStyle style, string? title = null)
    {
        TermFrame.Draw(_renderer, rect, style, title);
        return rect.Inner;
    }

    public TermRect CenterFrame(int contentW, int contentH)
    {
        return _area.CenterFrame(contentW, contentH);
    }

    public void RenderCanvas(PixelBuffer buffer, TerminalGraphicsMode mode, TermRect inner)
    {
        TerminalGraphicsRenderer.Render(_renderer, buffer, mode, inner.X, inner.Y, inner.W, inner.H);
    }

    public void RenderScreen(ScreenBuffer screen, int rows, int cols, TermRect inner)
    {
        TermArea.Screen(_renderer, inner, screen, rows, cols);
    }

    public static (int Cols, int Rows) FitImage(PixelBuffer img, int maxCols, int maxRows, TerminalGraphicsMode mode)
    {
        double pcc = mode is TerminalGraphicsMode.BrailleMono or TerminalGraphicsMode.BestGlyph or TerminalGraphicsMode.BestGlyphTrueColor ? 2.0 : 1.0;
        double prc = mode switch
        {
            TerminalGraphicsMode.HalfBlockColor => 2.0,
            TerminalGraphicsMode.BrailleMono => 4.0,
            TerminalGraphicsMode.Grayscale => 2.0,
            TerminalGraphicsMode.BestGlyph => 4.0,
            TerminalGraphicsMode.BestGlyphTrueColor => 4.0,
            _ => 1.0
        };
        double sc = Math.Min(maxCols * pcc / img.Width, maxRows * prc / img.Height);
        sc = Math.Min(1.0, Math.Max(sc, 0.01));
        return (
            Math.Clamp((int)Math.Ceiling(img.Width * sc / pcc), 1, maxCols),
            Math.Clamp((int)Math.Ceiling(img.Height * sc / prc), 1, maxRows));
    }

    public void InvalidateArea(TermRect rect)
    {
        if (_renderer is AnsiTerminalRenderer atr)
            atr.InvalidateArea(rect.X, rect.Y, rect.W, rect.H);
    }
}
