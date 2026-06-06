using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Rendering;

/// <summary>
/// Managed widget area with automatic frame, centering, and artifact-free transitions.
/// When position/size changes, old area is auto-cleared before drawing new frame.
/// </summary>
public sealed class TermWidget
{
    private readonly ITerminalRenderer _renderer;
    private int _oldX, _oldY, _oldW, _oldH;
    private bool _hasOld;

    public TermWidget(ITerminalRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>Outer frame position.</summary>
    public int X => _oldX;
    public int Y => _oldY;
    public int W => _oldW;
    public int H => _oldH;

    /// <summary>Inner content area.</summary>
    public int CX => _oldX + 1;
    public int CY => _oldY + 1;
    public int CW => _oldW - 2;
    public int CH => _oldH - 2;

    /// <summary>
    /// Center the frame in the given terminal area for the given content size.
    /// Automatically clears previous position if it changed.
    /// </summary>
    public void Center(TermRect terminalArea, int contentW, int contentH)
    {
        int newW = contentW + 2;
        int newH = contentH + 2;
        int newX = Math.Max(1, (terminalArea.W - newW) / 2);
        int newY = Math.Max(1, (terminalArea.H - newH) / 2);

        if (_hasOld && (newX != _oldX || newY != _oldY || newW != _oldW || newH != _oldH))
            ClearOldArea(terminalArea);

        _oldX = newX; _oldY = newY; _oldW = newW; _oldH = newH;
        _hasOld = true;
    }

    /// <summary>
    /// Draw the frame border. Call after Center().
    /// </summary>
    public void DrawFrame(FrameStyle style, string? title = null)
    {
        var g = style == FrameStyle.Unicode ? FrameGlyphs.Unicode : FrameGlyphs.Ascii;
        var fg = ConsoleColor.DarkCyan; var bg = ConsoleColor.Black;

        _renderer.SetCell(_oldX, _oldY, g.TopLeft, fg, bg);
        _renderer.SetCell(_oldX + _oldW - 1, _oldY, g.TopRight, fg, bg);
        _renderer.SetCell(_oldX, _oldY + _oldH - 1, g.BottomLeft, fg, bg);
        _renderer.SetCell(_oldX + _oldW - 1, _oldY + _oldH - 1, g.BottomRight, fg, bg);

        for (int c = _oldX + 1; c < _oldX + _oldW - 1; c++)
        {
            _renderer.SetCell(c, _oldY, g.Horizontal, fg, bg);
            _renderer.SetCell(c, _oldY + _oldH - 1, g.Horizontal, fg, bg);
        }
        for (int r = _oldY + 1; r < _oldY + _oldH - 1; r++)
        {
            _renderer.SetCell(_oldX, r, g.Vertical, fg, bg);
            _renderer.SetCell(_oldX + _oldW - 1, r, g.Vertical, fg, bg);
        }

        if (title != null)
            WriteTitle(title, fg, bg);
    }

    private void WriteTitle(string title, ConsoleColor frameFg, ConsoleColor frameBg)
    {
        title = $" {title} ";
        int maxLen = Math.Max(0, _oldW - 4);
        if (title.Length > maxLen) title = title[..maxLen];
        int titleX = _oldX + 2;
        for (int i = 0; i < title.Length; i++)
            _renderer.SetCell(titleX + i, _oldY, title[i], ConsoleColor.Cyan, frameBg);
    }

    /// <summary>Clear the inner content area.</summary>
    public void Clear(ConsoleColor fg, ConsoleColor bg)
    {
        for (int r = _oldY + 1; r < _oldY + _oldH - 1; r++)
            for (int c = _oldX + 1; c < _oldX + _oldW - 1; c++)
                _renderer.SetCell(c, r, ' ', fg, bg);
    }

    /// <summary>Set one cell inside the content area.</summary>
    public void SetCell(int col, int row, char ch, ConsoleColor fg, ConsoleColor bg)
    {
        _renderer.SetCell(_oldX + 1 + col, _oldY + 1 + row, ch, fg, bg);
    }

    /// <summary>Set text inside the content area.</summary>
    public void SetText(int col, int row, string text, ConsoleColor fg, ConsoleColor bg)
    {
        _renderer.SetText(_oldX + 1 + col, _oldY + 1 + row, text.AsSpan(), fg, bg);
    }

    /// <summary>
    /// Render a PixelCanvas into the content area with the given graphics mode.
    /// Automatically scales to fit.
    /// </summary>
    public void DrawCanvas(PixelCanvas canvas, TerminalGraphicsMode mode)
    {
        TerminalGraphicsRenderer.Render(_renderer, canvas.Buffer, mode, _oldX + 1, _oldY + 1, CW, CH);
    }

    /// <summary>
    /// Render ScreenBuffer into the content area.
    /// </summary>
    public void DrawScreenBuffer(ScreenBuffer screen, int rows, int cols)
    {
        int sx = _oldX + 1, sy = _oldY + 1;
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                char ch = screen.GetChar(col, row);
                _renderer.SetCell(sx + col, sy + row, ch == '\0' ? ' ' : ch,
                    screen.GetForeground(col, row), screen.GetBackground(col, row));
            }
    }

    /// <summary>
    /// Render text lines (centered) inside the content area.
    /// </summary>
    public void DrawTextCentered(string[] lines, ConsoleColor fg, ConsoleColor bg)
    {
        int top = Math.Max(0, (CH - lines.Length) / 2);
        for (int i = 0; i < lines.Length && top + i < CH; i++)
        {
            string line = lines[i].Length > CW ? lines[i][..CW] : lines[i];
            int left = Math.Max(0, (CW - line.Length) / 2);
            SetText(left, top + i, line, fg, bg);
        }
    }

    private void ClearOldArea(TermRect term)
    {
        // Clear the old area bounds to prevent artifacts
        int x1 = Math.Max(0, _oldX);
        int y1 = Math.Max(0, _oldY);
        int x2 = Math.Min(term.W, _oldX + _oldW);
        int y2 = Math.Min(term.H, _oldY + _oldH);
        for (int r = y1; r < y2; r++)
            for (int c = x1; c < x2; c++)
                _renderer.SetCell(c, r, ' ', ConsoleColor.Gray, ConsoleColor.Black);
    }
}

/// <summary>Terminal rectangle.</summary>
public readonly record struct TermRect(int W, int H);
