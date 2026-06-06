using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public class App
{
    private readonly ScreenBuffer _screen;
    private readonly EchoTerminal _echo;
    private readonly PiaDevice _pia;
    private readonly PiaTerminalAdapter _piaAdapter;
    private readonly ITerminalRenderer _renderer;
    private bool _running;
    private bool _showHelp;
    private string _statusText = "Ready";
    private TerminalLayout _lastLayout;
    private bool _dirty = true;
    private bool _fullRedraw = true;
    private bool _echoMode;
    private bool _demoMenu;
    private bool _demoRunning;
    private int _demoIndex;
    private string[] _demoBuffer = [];
    private int _demoLine;
    private int _demoChar;
    private int _termWidth;
    private int _termHeight;
    private ScreenMode _screenMode = ScreenMode.Rows25Cols80;

    private const int CompactWidth = 40;
    private const int WideWidth = 80;
    private const int ShortHeight = 24;
    private const int TallHeight = 25;
    private const int FunctionBarHeight = 1;

    public App()
    {
        _screen = new ScreenBuffer();
        _echo = new EchoTerminal(_screen);
        _pia = new PiaDevice(0x8800);
        _piaAdapter = new PiaTerminalAdapter(_pia, _screen);
        _renderer = new AnsiTerminalRenderer(Console.OpenStandardOutput());
        SeedScreen();
    }

    private ScreenSize GetRequestedScreenSize()
    {
        return _screenMode switch
        {
            ScreenMode.Rows24Cols40 => new ScreenSize(24, 40),
            ScreenMode.Rows25Cols40 => new ScreenSize(25, 40),
            _ => new ScreenSize(25, 80)
        };
    }

    public void Run()
    {
        Console.TreatControlCAsInput = true;
        _running = true;

        try
        {
            while (_running)
            {
                TerminalLayout layout = TerminalLayout.From(Console.WindowWidth, Console.WindowHeight);
                bool layoutChanged = layout != _lastLayout;
                if (layoutChanged)
                {
                    _termWidth = layout.Width;
                    _termHeight = layout.Height;
                    _renderer.Resize(_termWidth, _termHeight);
                }

                if (_dirty || layoutChanged)
                {
                    Render(layout, layoutChanged || _fullRedraw);
                    _lastLayout = layout;
                    _dirty = false;
                    _fullRedraw = false;
                }

                ReadInput();

                if (_demoRunning)
                    TickDemo();

                if (_echoMode)
                {
                    if (_echo.TickBlink())
                        _dirty = true;
                }

                Thread.Sleep(20);
            }
        }
        finally
        {
            _renderer.Dispose();
            Console.CursorVisible = true;
            Console.Clear();
        }
    }

    private void ReadInput()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            if (_demoMenu)
            {
                HandleDemoMenuKey(key);
                continue;
            }

            if (_echoMode)
            {
                if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.F3)
                {
                    _echoMode = false;
                    _statusText = "Ready";
                    _dirty = true;
                    _fullRedraw = true;
                }
                else
                {
                    _echo.ProcessKey(key.Key, key.KeyChar);
                    _dirty = true;
                }
                continue;
            }

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _running = false;
                    break;
                case ConsoleKey.F1:
                    _showHelp = !_showHelp;
                    _statusText = _showHelp ? "Help" : "Ready";
                    _dirty = true;
                    _fullRedraw = true;
                    break;
                case ConsoleKey.F2:
                    CycleScreenMode();
                    _dirty = true;
                    _fullRedraw = true;
                    break;
                case ConsoleKey.F3:
                    _echoMode = true;
                    _echo.Reset();
                    _statusText = "Echo";
                    _dirty = true;
                    _fullRedraw = true;
                    break;
                case ConsoleKey.F4:
                    _demoMenu = true;
                    _demoIndex = 0;
                    _statusText = "Demo";
                    _dirty = true;
                    _fullRedraw = true;
                    break;
                case ConsoleKey.F5:
                    _statusText = "Refresh";
                    _dirty = true;
                    _fullRedraw = true;
                    break;
                default:
                    _statusText = key.Key.ToString();
                    _dirty = true;
                    break;
            }
        }
    }

    private void HandleDemoMenuKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.F4:
                _demoMenu = false;
                _demoRunning = false;
                _statusText = "Ready";
                _dirty = true;
                _fullRedraw = true;
                break;
            case ConsoleKey.UpArrow:
                if (_demoIndex > 0) _demoIndex--;
                _dirty = true;
                break;
            case ConsoleKey.DownArrow:
                if (_demoIndex < PiaDemos.Names.Length - 1) _demoIndex++;
                _dirty = true;
                break;
            case ConsoleKey.Enter:
                StartDemo(_demoIndex);
                _demoMenu = false;
                _dirty = true;
                _fullRedraw = true;
                break;
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

    private void StartDemo(int index)
    {
        _screen.Reset();
        _pia.Reset();
        _piaAdapter.Reset();

        string text = PiaDemos.GetText(index);
        _demoBuffer = text.Split('\n');
        _demoLine = 0;
        _demoChar = 0;
        _demoRunning = true;
        _statusText = PiaDemos.Names[index];
    }

    private void TickDemo()
    {
        if (!_demoRunning) return;

        while (_demoLine < _demoBuffer.Length)
        {
            if (_demoChar < _demoBuffer[_demoLine].Length)
            {
                char ch = _demoBuffer[_demoLine][_demoChar];
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRA), (byte)ch);
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRB), 0x08);
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRB), 0x00);
                _demoChar++;
            }
            else
            {
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRA), 0x0D);
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRB), 0x08);
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRB), 0x00);
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRA), 0x0A);
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRB), 0x08);
                _pia.Write((ushort)(_pia.BaseAddress + PiaDevice.PRB), 0x00);
                _demoLine++;
                _demoChar = 0;
            }
        }

        _demoRunning = false;
        _dirty = true;
    }

    private void Render(TerminalLayout layout, bool fullRedraw)
    {
        if (fullRedraw)
            _renderer.Clear(ConsoleColor.Gray, ConsoleColor.Black);

        if (!layout.CanRender)
        {
            RenderTooSmall(layout);
            _renderer.Flush();
            return;
        }

        if (_demoMenu)
            RenderDemoMenu(layout);
        else if (_showHelp)
            RenderHelp(layout);
        else
            RenderScreen(layout);

        RenderFunctionBar(layout);
        _renderer.Flush();
    }

    private void RenderTooSmall(TerminalLayout layout)
    {
        WriteText(0, 0, "Terminal too small", ConsoleColor.White, ConsoleColor.DarkRed);
        WriteText(0, 1, $"Current: {layout.Width}x{layout.Height}", ConsoleColor.Gray, ConsoleColor.Black);
        WriteText(0, 2, "Minimum: 40x25", ConsoleColor.Gray, ConsoleColor.Black);
        WriteText(0, Math.Max(0, layout.Height - 1), "Esc Quit", ConsoleColor.Black, ConsoleColor.Gray);
    }

    private void RenderScreen(TerminalLayout layout)
    {
        ScreenSize requested = GetRequestedScreenSize();
        ScreenSize actual = layout.Fit(requested);

        int frameW = actual.Cols + 2;
        int frameH = actual.Rows + 2;

        int left = Math.Max(1, (layout.Width - frameW) / 2);
        int top = Math.Max(1, (layout.ContentHeight - frameH) / 2);

        DrawFrame(left, top, frameW, frameH);

        int screenLeft = left + 1;
        int screenTop = top + 1;

        for (int row = 0; row < actual.Rows; row++)
        {
            for (int col = 0; col < actual.Cols; col++)
            {
                char ch = _screen.GetChar(col, row);
                ConsoleColor fg = _screen.GetForeground(col, row);
                ConsoleColor bg = _screen.GetBackground(col, row);
                _renderer.SetCell(screenLeft + col, screenTop + row, ch == '\0' ? ' ' : ch, fg, bg);
            }
        }

        if (_echoMode && _echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, actual.Cols - 1);
            int cy = Math.Min(_echo.CursorY, actual.Rows - 1);
            char curCh = _screen.GetChar(cx, cy);
            _renderer.SetCell(screenLeft + cx, screenTop + cy, curCh == '\0' ? ' ' : curCh, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    private void DrawFrame(int left, int top, int w, int h)
    {
        ConsoleColor f = ConsoleColor.DarkCyan;
        ConsoleColor b = ConsoleColor.Black;

        _renderer.SetCell(left, top, '+', f, b);
        _renderer.SetCell(left + w - 1, top, '+', f, b);
        _renderer.SetCell(left, top + h - 1, '+', f, b);
        _renderer.SetCell(left + w - 1, top + h - 1, '+', f, b);

        for (int c = left + 1; c < left + w - 1; c++)
        {
            _renderer.SetCell(c, top, '-', f, b);
            _renderer.SetCell(c, top + h - 1, '-', f, b);
        }

        for (int r = top + 1; r < top + h - 1; r++)
        {
            _renderer.SetCell(left, r, '|', f, b);
            _renderer.SetCell(left + w - 1, r, '|', f, b);
        }
    }

    private void RenderHelp(TerminalLayout layout)
    {
        string[] lines =
        [
            "CPU-VIBE terminal",
            "",
            "F1  Help",
            "F2  Cycle screen: 24x40 / 25x40 / 25x80",
            "F3  Toggle echo mode (typing)",
            "F4  Demo menu",
            "F5  Refresh",
            "Esc Quit",
            "",
            "Echo mode: type text, arrows move cursor.",
            "F3 or Esc exits echo mode.",
            "",
            "Demos send data through PIA to terminal."
        ];

        int top = Math.Max(0, (layout.ContentHeight - lines.Length) / 2);
        for (int i = 0; i < lines.Length && top + i < layout.ContentHeight; i++)
        {
            string line = Trim(lines[i], layout.Width);
            int left = Math.Max(0, (layout.Width - line.Length) / 2);
            WriteText(left, top + i, line, i == 0 ? ConsoleColor.Cyan : ConsoleColor.Gray, ConsoleColor.Black);
        }
    }

    private void RenderDemoMenu(TerminalLayout layout)
    {
        string[] names = PiaDemos.Names;
        string title = "PIA Demos";
        int titleLeft = Math.Max(0, (layout.Width - title.Length) / 2);
        int top = Math.Max(1, (layout.ContentHeight - names.Length - 2) / 2);

        WriteText(titleLeft, top, title, ConsoleColor.Cyan, ConsoleColor.Black);
        top += 2;

        for (int i = 0; i < names.Length; i++)
        {
            bool selected = i == _demoIndex;
            string prefix = selected ? " > " : "   ";
            string line = prefix + names[i];
            int left = Math.Max(0, (layout.Width - line.Length) / 2);
            ConsoleColor fg = selected ? ConsoleColor.Black : ConsoleColor.Gray;
            ConsoleColor bg = selected ? ConsoleColor.Gray : ConsoleColor.Black;
            WriteText(left, top + i, line, fg, bg);
        }

        string hint = "Enter: run  Esc: back";
        int hintLeft = Math.Max(0, (layout.Width - hint.Length) / 2);
        WriteText(hintLeft, top + names.Length + 1, hint, ConsoleColor.DarkGray, ConsoleColor.Black);
    }

    private void RenderFunctionBar(TerminalLayout layout)
    {
        ScreenSize size = GetRequestedScreenSize();
        string left = _echoMode
            ? " F3 Normal  Esc Quit "
            : $" F1 Help  F2 {size.Rows}x{size.Cols}  F3 Echo  F4 Demo  F5 Refresh  Esc Quit ";
        string right = $" {_statusText} ";
        string bar = layout.Width >= left.Length + right.Length
            ? left + new string(' ', layout.Width - left.Length - right.Length) + right
            : left;

        WriteText(0, layout.Height - 1, Trim(bar, layout.Width).PadRight(layout.Width), ConsoleColor.Black, ConsoleColor.Gray);
    }

    private void WriteText(int col, int row, string text, ConsoleColor fg, ConsoleColor bg)
    {
        _renderer.SetText(col, row, text.AsSpan(), fg, bg);
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
