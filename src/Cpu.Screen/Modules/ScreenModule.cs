using Cpu.Module;
using Cpu.Screen.Rendering.Views;
using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Screen.Modules;

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
        switch (key.Key)
        {
            case ConsoleKey.F2:
                _screenMode = _screenMode switch
                {
                    ScreenMode.Rows25Cols80 => ScreenMode.Rows24Cols40,
                    ScreenMode.Rows24Cols40 => ScreenMode.Rows25Cols40,
                    _ => ScreenMode.Rows25Cols80
                };
                return true;
            case ConsoleKey.F3:
                _echoMode = !_echoMode;
                return true;
            case ConsoleKey.F5: return true;
            case ConsoleKey.F6:
                _frameStyle = _frameStyle == FrameStyle.Ascii ? FrameStyle.Unicode : FrameStyle.Ascii;
                return true;
        }
        return false;
    }

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        var mode = _echoMode ? "Echo" : _screenMode.ToString();
        var style = _frameStyle == FrameStyle.Unicode ? "Unicode" : "ASCII";
        _screenView.Render(r, new TermRect(0, 0, w, h - 1),
            new PresentationSession(r, new TermRect(0, 0, w, h - 1)));
    }
}
