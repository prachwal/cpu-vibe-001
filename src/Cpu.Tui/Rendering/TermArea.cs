using Cpu.Tui.Graphics;

using Cpu.Tui.Diagnostics;

namespace Cpu.Tui.Rendering;

/// <summary>
/// Content area operations: clear, write text, render canvas/screen.
/// Stateless — operates on any TermRect.
/// </summary>
public static class TermArea
{
    /// <summary>Fill area with spaces.</summary>
    public static void Clear(ITerminalRenderer r, TermRect area, ConsoleColor fg = ConsoleColor.Gray, ConsoleColor bg = ConsoleColor.Black)
    {
        RenderLog.Event("TermArea.Clear", $"rect={area} fg={fg} bg={bg}");
        for (int y = area.Y; y < area.Y2; y++)
            for (int x = area.X; x < area.X2; x++)
                r.SetCell(x, y, ' ', fg, bg);
    }

    public static void Clear(ITerminalRenderer r, TermRect area, TerminalCell cell, string source)
    {
        RenderLog.Event("TermArea.Clear", $"source={source} rect={area} cell={cell.Ch}");
        for (int y = area.Y; y < area.Y2; y++)
            for (int x = area.X; x < area.X2; x++)
                r.SetCell(x, y, cell.Ch, cell.Fg, cell.Bg);
    }

    /// <summary>Clear with named source for diagnostics.</summary>
    public static void Clear(ITerminalRenderer r, TermRect area, string source, ConsoleColor fg = ConsoleColor.Gray, ConsoleColor bg = ConsoleColor.Black)
    {
        RenderLog.Event("TermArea.Clear", $"source={source} rect={area}");
        for (int y = area.Y; y < area.Y2; y++)
            for (int x = area.X; x < area.X2; x++)
                r.SetCell(x, y, ' ', fg, bg);
    }

    /// <summary>Write text at position relative to area.</summary>
    public static void Write(ITerminalRenderer r, TermRect area, int x, int y, string text, ConsoleColor fg, ConsoleColor bg)
    {
        r.SetText(area.X + x, area.Y + y, text.AsSpan(), fg, bg);
    }

    /// <summary>Write text lines centered in area.</summary>
    public static void Centered(ITerminalRenderer r, TermRect area, string[] lines, ConsoleColor fg, ConsoleColor bg)
    {
        int top = Math.Max(0, (area.H - lines.Length) / 2);
        for (int i = 0; i < lines.Length && top + i < area.H; i++)
        {
            string line = lines[i].Length > area.W ? lines[i][..area.W] : lines[i];
            int left = Math.Max(0, (area.W - line.Length) / 2);
            r.SetText(area.X + left, area.Y + top + i, line.AsSpan(), fg, bg);
        }
    }

    /// <summary>Render PixelCanvas with given graphics mode into area.</summary>
    public static void Canvas(ITerminalRenderer r, TermRect area, PixelCanvas canvas, TerminalGraphicsMode mode)
    {
        TerminalGraphicsRenderer.Render(r, canvas.Buffer, mode, area.X, area.Y, area.W, area.H);
    }

    /// <summary>Render ScreenBuffer text into area.</summary>
    public static void Screen(ITerminalRenderer r, TermRect area, ScreenBuffer screen, int rows, int cols)
    {
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                char ch = screen.GetChar(col, row);
                r.SetCell(area.X + col, area.Y + row, ch == '\0' ? ' ' : ch,
                    screen.GetForeground(col, row), screen.GetBackground(col, row));
            }
    }
}
