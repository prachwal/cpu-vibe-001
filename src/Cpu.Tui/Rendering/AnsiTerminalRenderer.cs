using Cpu.Tui.Diagnostics;

namespace Cpu.Tui.Rendering;

/// <summary>
/// ANSI terminal renderer with double buffering and chunked output.
/// Generates ANSI escape sequences, flushing to stdout in configurable chunks.
/// Supports both ConsoleColor and truecolor RGB.
/// </summary>
public sealed class AnsiTerminalRenderer : ITerminalRenderer
{
    private readonly Stream _stdout;
    private TerminalCell[] _front = [];
    private TerminalCell[] _back = [];
    private byte[] _output = new byte[128 * 1024]; // 128KB chunk — safe for full-screen truecolor
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    public AnsiTerminalRenderer(Stream stdout)
    {
        _stdout = stdout;
        WriteRaw("\x1b[?25l");
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _front = new TerminalCell[width * height];
        _back = new TerminalCell[width * height];
        Array.Fill(_front, TerminalCell.Unknown);
        Array.Fill(_back, TerminalCell.Empty);
        WriteRaw("\x1b[2J\x1b[H");
    }

    [Obsolete("Use SetCell with TerminalColor")]
    public void SetCell(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg)
    {
        if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) return;
        _back[y * _width + x] = new TerminalCell(ch,
            TerminalColor.FromConsole(fg),
            TerminalColor.FromConsole(bg));
    }

    public void SetCell(int x, int y, char ch, TerminalColor fg, TerminalColor bg)
    {
        if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) return;
        _back[y * _width + x] = new TerminalCell(ch, fg, bg);
    }

    public void SetText(int x, int y, ReadOnlySpan<char> text, ConsoleColor fg, ConsoleColor bg)
    {
        var tFg = TerminalColor.FromConsole(fg);
        var tBg = TerminalColor.FromConsole(bg);
        for (int i = 0; i < text.Length; i++)
            SetCell(x + i, y, text[i], tFg, tBg);
    }

    public void Clear(ConsoleColor fg, ConsoleColor bg)
    {
        var fill = new TerminalCell(' ',
            TerminalColor.FromConsole(fg),
            TerminalColor.FromConsole(bg));
        Array.Fill(_back, fill);
    }

    public void Flush()
    {
        AnsiBuilder builder = new(_output);
        TerminalColor curFg = TerminalColor.FromConsole(ConsoleColor.Black);
        TerminalColor curBg = TerminalColor.FromConsole(ConsoleColor.Black);
        int changedCount = 0, skippedCount = 0;

        for (int y = 0; y < _height; y++)
        {
            int x = 0;
            while (x < _width)
            {
                int index = y * _width + x;
                if (_front[index] == _back[index])
                {
                    x++;
                    skippedCount++;
                    continue;
                }

                changedCount++;

                // Flush if not enough room for worst-case cell group
                if (builder.Length > _output.Length - AnsiBuilder.MaxCellBytes)
                    FlushBuffer(ref builder);

                TerminalCell cell = _back[index];
                builder.MoveTo(x, y);
                builder.SetColor(cell.Fg, cell.Bg, ref curFg, ref curBg);

                while (x < _width)
                {
                    index = y * _width + x;
                    TerminalCell next = _back[index];
                    if (_front[index] == next) break;
                    if (next.Fg != curFg || next.Bg != curBg) break;

                    // Flush before character if buffer is tight
                    if (builder.Length > _output.Length - 16)
                        FlushBuffer(ref builder);

                    builder.WriteChar(next.Ch);
                    _front[index] = next;
                    x++;
                }
            }
        }

        if (builder.Length > 0)
            FlushBuffer(ref builder);
        RenderLog.Event("AnsiTerminalRenderer.Flush",
            $"changed={changedCount} skipped={skippedCount} bytes={builder.Length}");
    }

    private void FlushBuffer(ref AnsiBuilder builder)
    {
        _stdout.Write(_output, 0, builder.Length);
        _stdout.Flush();
        builder.Reset();
    }

    public void Dispose()
    {
        WriteRaw("\x1b[0m\x1b[?25h");
    }

    private void WriteRaw(string sequence)
    {
        byte[] bytes = new byte[sequence.Length];
        for (int i = 0; i < sequence.Length; i++)
            bytes[i] = (byte)sequence[i];
        _stdout.Write(bytes, 0, bytes.Length);
        _stdout.Flush();
    }

    /// <summary>
    /// Invalidate front buffer for a rectangle — forces Flush to output those cells.
    /// Call after switching content/mode to prevent Front==Back from skipping cells.
    /// </summary>
    public void InvalidateArea(int x, int y, int w, int h, string reason = "")
    {
        int cells = 0;
        for (int row = y; row < y + h && row < _height; row++)
            for (int col = x; col < x + w && col < _width; col++)
            {
                _front[row * _width + col] = TerminalCell.Unknown;
                cells++;
            }
        RenderLog.Event("AnsiTerminalRenderer.InvalidateArea", $"rect=({x},{y},{w},{h}) cells={cells} reason=\"{reason}\"");
    }
}
