using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Screen.Rendering.Views;

public class ScreenView : BaseTermView
{
    private readonly ScreenBuffer _screen;
    private readonly EchoTerminal _echo;
    public override string Name => "Screen";
    public EchoTerminal Echo => _echo;
    public bool EchoEnabled { get; set; }
    public ScreenMode ScreenMode { get; set; } = ScreenMode.Rows25Cols80;

    public ScreenView(ScreenBuffer screen, EchoTerminal echo)
    {
        _screen = screen; _echo = echo;
    }

    protected override void Seed() { }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        var layout = TerminalLayout.From(area.W, area.H);
        var sz = layout.Fit(ScreenModeToSize(ScreenMode));
        int cols = sz.Cols;
        int rows = sz.Rows;
        var frame = session.CenterFrame(cols, rows);
        session.DrawFrame(frame, FrameStyle.Ascii);
        session.RenderScreen(_screen, rows, cols, frame.Inner);
        if (EchoEnabled && _echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, cols - 1), cy = Math.Min(_echo.CursorY, rows - 1);
            char cur = _screen.GetChar(cx, cy);
            r.SetCell(frame.X + 1 + cx, frame.Y + 1 + cy,
                cur == '\0' ? ' ' : cur, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    private static ScreenSize ScreenModeToSize(ScreenMode mode) => mode switch
    {
        ScreenMode.Rows24Cols40 => new ScreenSize(24, 40),
        ScreenMode.Rows25Cols40 => new ScreenSize(25, 40),
        _ => new ScreenSize(25, 80)
    };
}
