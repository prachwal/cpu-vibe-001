namespace Cpu.Tui;

public class App
{
    private readonly ScreenBuffer _screen;
    private bool _running;
    private bool _showHelp;
    private string _statusText = "Ready";
    private TerminalLayout _lastLayout;
    private bool _dirty = true;

    private const int CompactWidth = 40;
    private const int WideWidth = 80;
    private const int ShortHeight = 24;
    private const int TallHeight = 25;
    private const int FunctionBarHeight = 1;

    public App()
    {
        _screen = new ScreenBuffer();
        SeedScreen();
    }

    public void Run()
    {
        Console.TreatControlCAsInput = true;
        Console.CursorVisible = false;
        _running = true;

        try
        {
            while (_running)
            {
                TerminalLayout layout = TerminalLayout.From(Console.WindowWidth, Console.WindowHeight);
                if (_dirty || layout != _lastLayout)
                {
                    Render(layout);
                    _lastLayout = layout;
                    _dirty = false;
                }

                ReadInput();
                Thread.Sleep(20);
            }
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = true;
            Console.Clear();
        }
    }

    private void ReadInput()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _running = false;
                    break;
                case ConsoleKey.F1:
                    _showHelp = !_showHelp;
                    _statusText = _showHelp ? "Help" : "Ready";
                    _dirty = true;
                    break;
                case ConsoleKey.F2:
                    CycleScreenMode();
                    _dirty = true;
                    break;
                case ConsoleKey.F5:
                    _statusText = "Refresh";
                    _dirty = true;
                    break;
                default:
                    _statusText = key.Key.ToString();
                    _dirty = true;
                    break;
            }
        }
    }

    private void CycleScreenMode()
    {
        if (_screenMode == ScreenMode.Rows24Cols40)
            _screenMode = ScreenMode.Rows25Cols40;
        else if (_screenMode == ScreenMode.Rows25Cols40)
            _screenMode = ScreenMode.Rows25Cols80;
        else
            _screenMode = ScreenMode.Rows24Cols40;

        _statusText = $"{GetRequestedScreenSize().Rows}x{GetRequestedScreenSize().Cols}";
    }

    private ScreenMode _screenMode = ScreenMode.Rows25Cols80;

    private ScreenSize GetRequestedScreenSize()
    {
        return _screenMode switch
        {
            ScreenMode.Rows24Cols40 => new ScreenSize(24, 40),
            ScreenMode.Rows25Cols40 => new ScreenSize(25, 40),
            _ => new ScreenSize(25, 80)
        };
    }

    private void Render(TerminalLayout layout)
    {
        Console.SetCursorPosition(0, 0);

        if (!layout.CanRender)
        {
            RenderTooSmall(layout);
            return;
        }

        if (_showHelp)
            RenderHelp(layout);
        else
            RenderScreen(layout);

        RenderFunctionBar(layout);
    }

    private void RenderTooSmall(TerminalLayout layout)
    {
        Console.Clear();
        WriteAt(0, 0, "Terminal too small", ConsoleColor.White, ConsoleColor.DarkRed, layout.Width);
        WriteAt(0, 1, $"Current: {layout.Width}x{layout.Height}", ConsoleColor.Gray, ConsoleColor.Black, layout.Width);
        WriteAt(0, 2, "Minimum: 40x25", ConsoleColor.Gray, ConsoleColor.Black, layout.Width);
        WriteAt(0, Math.Max(0, layout.Height - 1), "Esc Quit", ConsoleColor.Black, ConsoleColor.Gray, layout.Width);
    }

    private void RenderScreen(TerminalLayout layout)
    {
        ScreenSize requested = GetRequestedScreenSize();
        ScreenSize actual = layout.Fit(requested);

        // ramka zabiera 1 wiersz z góry, 1 z dołu, 1 kolumnę z lewej, 1 z prawej
        int frameW = actual.Cols + 2;
        int frameH = actual.Rows + 2;

        int left = Math.Max(1, (layout.Width - frameW) / 2);
        int top = Math.Max(1, (layout.ContentHeight - frameH) / 2);

        ClearContent(layout);

        DrawFrame(left, top, frameW, frameH, actual.Cols, actual.Rows);

        int screenLeft = left + 1;
        int screenTop = top + 1;

        for (int row = 0; row < actual.Rows; row++)
        {
            int sourceRow = row;
            for (int col = 0; col < actual.Cols; col++)
            {
                char ch = _screen.GetChar(col, sourceRow);
                ConsoleColor fg = _screen.GetForeground(col, sourceRow);
                ConsoleColor bg = _screen.GetBackground(col, sourceRow);
                WriteCharAt(screenLeft + col, screenTop + row, ch == '\0' ? ' ' : ch, fg, bg);
            }
        }

        string title = $" CPU-VIBE {actual.Rows}x{actual.Cols} ";
        WriteAt(screenLeft, screenTop, title[..Math.Min(title.Length, actual.Cols)], ConsoleColor.Black, ConsoleColor.Gray, actual.Cols);
    }

    private static void DrawFrame(int left, int top, int w, int h, int innerCols, int innerRows)
    {
        ConsoleColor frameFg = ConsoleColor.DarkCyan;
        ConsoleColor frameBg = ConsoleColor.Black;

        WriteCharAt(left, top, '┌', frameFg, frameBg);
        WriteCharAt(left + w - 1, top, '┐', frameFg, frameBg);
        WriteCharAt(left, top + h - 1, '└', frameFg, frameBg);
        WriteCharAt(left + w - 1, top + h - 1, '┘', frameFg, frameBg);

        for (int c = left + 1; c < left + w - 1; c++)
        {
            WriteCharAt(c, top, '─', frameFg, frameBg);
            WriteCharAt(c, top + h - 1, '─', frameFg, frameBg);
        }

        for (int r = top + 1; r < top + h - 1; r++)
        {
            WriteCharAt(left, r, '│', frameFg, frameBg);
            WriteCharAt(left + w - 1, r, '│', frameFg, frameBg);
        }
    }

    private void RenderHelp(TerminalLayout layout)
    {
        ClearContent(layout);
        string[] lines =
        {
            "CPU-VIBE terminal",
            "",
            "F1  Help",
            "F2  Cycle screen: 24x40 / 25x40 / 25x80",
            "F5  Refresh",
            "Esc Quit",
            "",
            "The emulated screen is centered.",
            "The function bar always stays at the bottom."
        };

        int top = Math.Max(0, (layout.ContentHeight - lines.Length) / 2);
        for (int i = 0; i < lines.Length && top + i < layout.ContentHeight; i++)
        {
            string line = Trim(lines[i], layout.Width);
            int left = Math.Max(0, (layout.Width - line.Length) / 2);
            WriteAt(left, top + i, line, i == 0 ? ConsoleColor.Cyan : ConsoleColor.Gray, ConsoleColor.Black, layout.Width - left);
        }
    }

    private void RenderFunctionBar(TerminalLayout layout)
    {
        ScreenSize size = GetRequestedScreenSize();
        string left = $" F1 Help  F2 {size.Rows}x{size.Cols}  F5 Refresh  Esc Quit ";
        string right = $" {_statusText} ";
        string bar = layout.Width >= left.Length + right.Length
            ? left + new string(' ', layout.Width - left.Length - right.Length) + right
            : left;

        WriteAt(0, layout.Height - 1, Trim(bar, layout.Width).PadRight(layout.Width), ConsoleColor.Black, ConsoleColor.Gray, layout.Width);
    }

    private void ClearContent(TerminalLayout layout)
    {
        for (int row = 0; row < layout.ContentHeight; row++)
            WriteAt(0, row, new string(' ', layout.Width), ConsoleColor.Gray, ConsoleColor.Black, layout.Width);
    }

    private static void WriteAt(int col, int row, string text, ConsoleColor fg, ConsoleColor bg, int maxWidth)
    {
        if (row < 0 || col < 0 || maxWidth <= 0) return;
        Console.SetCursorPosition(col, row);
        Console.ForegroundColor = fg;
        Console.BackgroundColor = bg;
        Console.Write(Trim(text, maxWidth));
    }

    private static void WriteCharAt(int col, int row, char ch, ConsoleColor fg, ConsoleColor bg)
    {
        Console.SetCursorPosition(col, row);
        Console.ForegroundColor = fg;
        Console.BackgroundColor = bg;
        Console.Write(ch);
    }

    private static string Trim(string text, int width)
    {
        if (width <= 0) return string.Empty;
        return text.Length <= width ? text : text[..width];
    }

    private void SeedScreen()
    {
        _screen.FillRect(0, 0, ScreenBuffer.Width, ScreenBuffer.Height, ' ', ConsoleColor.Gray, ConsoleColor.Black);
    }

    private enum ScreenMode
    {
        Rows24Cols40,
        Rows25Cols40,
        Rows25Cols80
    }

    private readonly record struct ScreenSize(int Rows, int Cols);

    private readonly record struct TerminalLayout(int Width, int Height)
    {
        public int ContentHeight => Math.Max(0, Height - FunctionBarHeight);
        public bool CanRender => Width >= CompactWidth && Height >= ShortHeight + FunctionBarHeight;

        public static TerminalLayout From(int width, int height)
        {
            return new TerminalLayout(Math.Max(1, width), Math.Max(1, height));
        }

        public ScreenSize Fit(ScreenSize requested)
        {
            int cols = Width >= WideWidth && requested.Cols == WideWidth ? WideWidth : CompactWidth;
            int rows = ContentHeight >= TallHeight && requested.Rows == TallHeight ? TallHeight : ShortHeight;
            return new ScreenSize(rows, cols);
        }
    }
}
