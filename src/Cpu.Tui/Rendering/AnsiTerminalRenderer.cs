namespace Cpu.Tui.Rendering;

/// <summary>
/// ANSI terminal renderer with double buffering.
/// Generates ANSI escape sequences and writes them in a single Stream.Write per flush.
/// </summary>
public sealed class AnsiTerminalRenderer : ITerminalRenderer
{
    private readonly Stream _stdout;
    private TerminalCell[] _front = [];
    private TerminalCell[] _back = [];
    private byte[] _output = new byte[64 * 1024];
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    public AnsiTerminalRenderer(Stream stdout)
    {
        _stdout = stdout;
        WriteRaw("\x1b[?25l"); // hide cursor
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _front = new TerminalCell[width * height];
        _back = new TerminalCell[width * height];
        Array.Fill(_front, TerminalCell.Unknown);
        Array.Fill(_back, TerminalCell.Empty);
        WriteRaw("\x1b[2J\x1b[H"); // clear + home
    }

    public void SetCell(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg)
    {
        if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) return;
        _back[y * _width + x] = new TerminalCell(ch, fg, bg);
    }

    public void SetText(int x, int y, ReadOnlySpan<char> text, ConsoleColor fg, ConsoleColor bg)
    {
        for (int i = 0; i < text.Length; i++)
            SetCell(x + i, y, text[i], fg, bg);
    }

    public void Clear(ConsoleColor fg, ConsoleColor bg)
    {
        TerminalCell fill = new(' ', fg, bg);
        Array.Fill(_back, fill);
    }

    public void Flush()
    {
        AnsiBuilder builder = new(_output);
        ConsoleColor curFg = ConsoleColor.Black;
        ConsoleColor curBg = ConsoleColor.Black;

        for (int y = 0; y < _height; y++)
        {
            int x = 0;
            while (x < _width)
            {
                int index = y * _width + x;
                if (_front[index] == _back[index])
                {
                    x++;
                    continue;
                }

                TerminalCell cell = _back[index];
                builder.MoveTo(x, y);
                builder.SetColor(cell.Fg, cell.Bg, ref curFg, ref curBg);

                while (x < _width)
                {
                    index = y * _width + x;
                    TerminalCell next = _back[index];
                    if (_front[index] == next) break;
                    if (next.Fg != curFg || next.Bg != curBg) break;

                    builder.WriteChar(next.Ch);
                    _front[index] = next;
                    x++;
                }
            }
        }

        if (builder.Length > 0)
        {
            _stdout.Write(_output, 0, builder.Length);
            _stdout.Flush();
        }
    }

    public void Dispose()
    {
        WriteRaw("\x1b[0m\x1b[?25h"); // reset + show cursor
    }

    private void WriteRaw(string sequence)
    {
        byte[] bytes = new byte[sequence.Length];
        for (int i = 0; i < sequence.Length; i++)
            bytes[i] = (byte)sequence[i];
        _stdout.Write(bytes, 0, bytes.Length);
        _stdout.Flush();
    }
}
