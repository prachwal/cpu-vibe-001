using Cpu.Module;
using Cpu.Screen.Rendering.Views;
using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Layout;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Screen.Modules;

public sealed class ScreenModule : ModuleBase
{
    private readonly ScreenBuffer _screen;
    private readonly EchoTerminal _echo;
    private readonly ScreenView _screenView;
    private ScreenMode _screenMode = ScreenMode.Rows25Cols80;
    private bool _echoMode;
    public override string Name => "Screen";
    private static readonly string[] ModeLabels = ["24x40", "25x40", "25x80"];

    public ScreenModule(ScreenBuffer screen, EchoTerminal echo, ITuiAppConfiguration config,
        ErrorCollector? errors = null) : base(config, errors)
    {
        _screen = screen;
        _echo = echo;
        _screenView = new ScreenView(screen, echo);
    }

    public override bool OnTick() => _echoMode && _echo.TickBlink();

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.F5:
                _screenMode = _screenMode switch
                {
                    ScreenMode.Rows25Cols80 => ScreenMode.Rows24Cols40,
                    ScreenMode.Rows24Cols40 => ScreenMode.Rows25Cols40,
                    _ => ScreenMode.Rows25Cols80
                };
                return true;
            case ConsoleKey.F6:
                _echoMode = !_echoMode;
                if (_echoMode) _echo.ShowCursor();
                return true;
        }
        if (_echoMode)
        {
            switch (key.Key)
            {
                case ConsoleKey.F1:
                case ConsoleKey.F2:
                case ConsoleKey.F4:
                case ConsoleKey.F7:
                case ConsoleKey.F8:
                case ConsoleKey.F9:
                case ConsoleKey.Escape:
                    return false;
            }
            _echo.ProcessKey(key.Key, key.KeyChar);
            return true;
        }
        return false;
    }

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        _screenView.EchoEnabled = _echoMode;
        _screenView.ScreenMode = _screenMode;
        SyncViewSettings(_screenView);
        _screenView.Render(r, new TermRect(x, y, w, h),
            new PresentationSession(r, new TermRect(x, y, w, h)));
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y)
    {
        string mode = _echoMode ? "Echo" : ModeLabels[(int)_screenMode];
        PanelLine(r, w, y++, $" Mode: {mode}");
        PanelLine(r, w, y++, $" Echo: {(_echoMode ? "ON" : "OFF")}");
        PanelLine(r, w, y++, $" Frame: {Config.Current.FrameStyle}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++; PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan); y++;
        PanelLine(r, w, y++, " F5  cycle mode");
        PanelLine(r, w, y++, " F6  echo toggle");
        PanelLine(r, w, y++, " F2  setup");
    }
}
