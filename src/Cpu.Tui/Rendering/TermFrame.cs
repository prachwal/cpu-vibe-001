namespace Cpu.Tui.Rendering;

/// <summary>
/// Draws a frame border with optional title.
/// Stateless — no position tracking. Use with any TermRect.
/// </summary>
public static class TermFrame
{
    /// <summary>
    /// Draw a frame border at the given rectangle.
    /// </summary>
    public static void Draw(ITerminalRenderer r, TermRect rect, FrameStyle style, string? title = null)
    {
        var g = style == FrameStyle.Unicode ? FrameGlyphs.Unicode : FrameGlyphs.Ascii;
        var fg = ConsoleColor.DarkCyan; var bg = ConsoleColor.Black;

        r.SetCell(rect.X, rect.Y, g.TopLeft, fg, bg);
        r.SetCell(rect.X2 - 1, rect.Y, g.TopRight, fg, bg);
        r.SetCell(rect.X, rect.Y2 - 1, g.BottomLeft, fg, bg);
        r.SetCell(rect.X2 - 1, rect.Y2 - 1, g.BottomRight, fg, bg);

        for (int c = rect.X + 1; c < rect.X2 - 1; c++)
        {
            r.SetCell(c, rect.Y, g.Horizontal, fg, bg);
            r.SetCell(c, rect.Y2 - 1, g.Horizontal, fg, bg);
        }
        for (int rr = rect.Y + 1; rr < rect.Y2 - 1; rr++)
        {
            r.SetCell(rect.X, rr, g.Vertical, fg, bg);
            r.SetCell(rect.X2 - 1, rr, g.Vertical, fg, bg);
        }

        if (title != null)
            WriteTitle(r, rect, title);
    }

    private static void WriteTitle(ITerminalRenderer r, TermRect rect, string title)
    {
        title = $" {title} ";
        int maxLen = Math.Max(0, rect.W - 4);
        if (title.Length > maxLen) title = title[..maxLen];
        int tx = rect.X + 2;
        for (int i = 0; i < title.Length; i++)
            r.SetCell(tx + i, rect.Y, title[i], ConsoleColor.Cyan, ConsoleColor.Black);
    }
}
