using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

/// <summary>
/// Fake terminal renderer for testing - captures SetCell calls.
/// </summary>
public sealed class FakeTerminalRenderer : ITerminalRenderer
{
    public int Width { get; }
    public int Height { get; }

    private readonly TerminalCell[] _cells;

    public FakeTerminalRenderer(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new TerminalCell[width * height];
        Array.Fill(_cells, TerminalCell.Unknown);
    }

    public void Resize(int width, int height)
    {
        // Not needed for tests
    }

    public void SetCell(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            _cells[y * Width + x] = new TerminalCell(ch, fg, bg);
        }
    }

    public void SetText(int x, int y, ReadOnlySpan<char> text, ConsoleColor fg, ConsoleColor bg)
    {
        for (int i = 0; i < text.Length; i++)
            SetCell(x + i, y, text[i], fg, bg);
    }

    public void Clear(ConsoleColor fg, ConsoleColor bg)
    {
        var fill = new TerminalCell(' ', fg, bg);
        Array.Fill(_cells, fill);
    }

    public void Flush()
    {
        // No-op for tests
    }

    public void Dispose()
    {
        // No-op
    }

    public TerminalCell GetCell(int x, int y) => _cells[y * Width + x];
}
