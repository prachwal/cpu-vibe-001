using Cpu.Module;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Tui.Modules;

public sealed class ScreenModule : IAppModule
{
    private readonly ScreenBuffer _screen;
    private readonly EchoTerminal _echo;
    private readonly ScreenView _screenView;
    private ScreenMode _screenMode = ScreenMode.Rows25Cols80;
    private bool _echoMode;
    private FrameStyle _frameStyle = FrameStyle.Ascii;
    private readonly TerminalLayout _layout = new(80, 25);

    public string Name => "Screen";
    public ConsoleKey? ActivateKey => null;
    public string ActivateLabel => "";
    public bool IsTransient => true;
    public bool ShowInBar => false;
    public bool IsActive { get; private set; }

    public ScreenModule(ScreenBuffer screen, EchoTerminal echo)
    {
        _screen = screen; _echo = echo;
        _screenView = new ScreenView(screen, echo);
    }

    public void OnActivate() { IsActive = true; }
    public void OnDeactivate() { IsActive = false; }
    public bool OnTick() => _echoMode && _echo.TickBlink();

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (_echoMode)
        {
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.F3) _echoMode = false;
            else _echo.ProcessKey(key.Key, key.KeyChar);
            return true;
        }
        switch (key.Key)
        {
            case ConsoleKey.F2: CycleMode(); return true;
            case ConsoleKey.F3: _echoMode = true; _echo.Reset(); return true;
            case ConsoleKey.F5: return true;
            case ConsoleKey.F6: _frameStyle = _frameStyle == FrameStyle.Ascii ? FrameStyle.Unicode : FrameStyle.Ascii; return true;
        }
        return false;
    }

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        var layout = TerminalLayout.From(w, h);
        var req = FitSize();
        var actual = layout.Fit(req);
        var area = new TermRect(0, 0, w, layout.ContentHeight);
        var s = new PresentationSession(r, area);
        var f = s.CenterFrame(actual.Cols, actual.Rows);
        s.Clear();
        s.DrawFrame(f, _frameStyle);
        s.RenderScreen(_screen, actual.Rows, actual.Cols, f.Inner);

        if (_echoMode && _echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, actual.Cols - 1), cy = Math.Min(_echo.CursorY, actual.Rows - 1);
            char cur = _screen.GetChar(cx, cy);
            r.SetCell(f.X + 1 + cx, f.Y + 1 + cy, cur == '\0' ? ' ' : cur, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    private ScreenSize FitSize() => _screenMode switch
    {
        ScreenMode.Rows24Cols40 => new ScreenSize(24, 40),
        ScreenMode.Rows25Cols40 => new ScreenSize(25, 40),
        _ => new ScreenSize(25, 80)
    };

    private void CycleMode()
    {
        _screenMode = _screenMode switch
        {
            ScreenMode.Rows24Cols40 => ScreenMode.Rows25Cols40,
            ScreenMode.Rows25Cols40 => ScreenMode.Rows25Cols80,
            _ => ScreenMode.Rows24Cols40
        };
    }
}
