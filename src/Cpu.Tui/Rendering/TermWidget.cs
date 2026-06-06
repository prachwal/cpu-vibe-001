using Cpu.Tui.Graphics;

namespace Cpu.Tui.Rendering;

/// <summary>
/// Managed widget area with automatic frame, centering, and artifact-free transitions.
/// Composes TermFrame + TermArea with position tracking.
/// </summary>
public sealed class TermWidget
{
    private readonly ITerminalRenderer _renderer;
    private TermRect _outer;
    private bool _hasOld;

    public TermWidget(ITerminalRenderer renderer) => _renderer = renderer;

    /// <summary>Outer frame position.</summary>
    public int X => _outer.X;
    public int Y => _outer.Y;
    public int W => _outer.W;
    public int H => _outer.H;
    public TermRect Outer => _outer;
    public TermRect Inner => _outer.Inner;

    /// <summary>Center the frame, auto-clearing old position if changed.</summary>
    public void Center(TermRect terminalArea, int contentW, int contentH)
    {
        var newOuter = terminalArea.CenterFrame(contentW, contentH);
        if (_hasOld && newOuter != _outer)
            TermArea.Clear(_renderer, _outer);
        _outer = newOuter;
        _hasOld = true;
    }

    /// <summary>Draw frame border with optional title.</summary>
    public void DrawFrame(FrameStyle style, string? title = null)
        => TermFrame.Draw(_renderer, _outer, style, title);

    /// <summary>Clear inner content area.</summary>
    public void Clear(ConsoleColor fg = ConsoleColor.Gray, ConsoleColor bg = ConsoleColor.Black)
        => TermArea.Clear(_renderer, _outer.Inner, fg, bg);

    /// <summary>Set cell inside content area.</summary>
    public void SetCell(int col, int row, char ch, ConsoleColor fg, ConsoleColor bg)
        => _renderer.SetCell(_outer.Inner.X + col, _outer.Inner.Y + row, ch, fg, bg);

    /// <summary>Write text inside content area.</summary>
    public void SetText(int col, int row, string text, ConsoleColor fg, ConsoleColor bg)
        => TermArea.Write(_renderer, _outer.Inner, col, row, text, fg, bg);

    /// <summary>Render PixelCanvas into content area.</summary>
    public void DrawCanvas(PixelCanvas canvas, TerminalGraphicsMode mode)
        => TermArea.Canvas(_renderer, _outer.Inner, canvas, mode);

    /// <summary>Render ScreenBuffer into content area.</summary>
    public void DrawScreenBuffer(ScreenBuffer screen, int rows, int cols)
        => TermArea.Screen(_renderer, _outer.Inner, screen, rows, cols);

    /// <summary>Render centered text lines in content area.</summary>
    public void DrawTextCentered(string[] lines, ConsoleColor fg, ConsoleColor bg)
        => TermArea.Centered(_renderer, _outer.Inner, lines, fg, bg);
}
