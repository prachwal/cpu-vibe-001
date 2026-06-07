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

    public void RenderScreen(ScreenBuffer screen, int rows, int cols, TermRect inner)
    {
        TermArea.Screen(_renderer, inner, screen, rows, cols);
    }

    public static (int Cols, int Rows) FitImage(PixelBuffer img, int maxCols, int maxRows, TerminalGraphicsMode mode) =>
        FitImage(img.Width, img.Height, maxCols, maxRows, mode);

    public static (int Cols, int Rows) FitImage(int imgWidth, int imgHeight, int maxCols, int maxRows, TerminalGraphicsMode _)
    {
        double pcc = TerminalGraphicsModes.FrameLayoutPixelsPerCellColumn;
        double prc = TerminalGraphicsModes.FrameLayoutPixelsPerCellRow;
        double sc = Math.Min(maxCols * pcc / imgWidth, maxRows * prc / imgHeight);
        sc = Math.Min(1.0, Math.Max(sc, 0.01));
        int cols = Math.Clamp((int)Math.Floor(imgWidth * sc / pcc), 1, maxCols);
        int rows = Math.Clamp((int)Math.Floor(imgHeight * sc / prc), 1, maxRows);
        return (cols, rows);
    }

    public static (PixelBuffer Scaled, int Cols, int Rows) PrepareCanvas(
        PixelBuffer source,
        int maxCols,
        int maxRows,
        TerminalGraphicsMode mode)
    {
        (int cols, int rows) = FitImage(source, maxCols, maxRows, mode);
        PixelBuffer scaled = TerminalGraphicsRenderer.ScaleForCells(source, cols, rows, mode);
        return (scaled, cols, rows);
    }

    public void RenderCanvas(PixelBuffer buffer, TerminalGraphicsMode mode, TermRect inner)
    {
        PixelBuffer scaled = TerminalGraphicsRenderer.ScaleForCells(buffer, inner.W, inner.H, mode);
        TerminalGraphicsRenderer.RenderScaled(_renderer, scaled, mode, inner.X, inner.Y, inner.W, inner.H);
    }

    public void InvalidateArea(TermRect rect)
    {
        if (_renderer is AnsiTerminalRenderer atr)
            atr.InvalidateArea(rect.X, rect.Y, rect.W, rect.H);
    }
}
