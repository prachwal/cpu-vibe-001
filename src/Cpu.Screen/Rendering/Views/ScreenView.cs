using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
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

    public ScreenView(ScreenBuffer screen, EchoTerminal echo)
    {
        _screen = screen; _echo = echo;
    }

    protected override void Seed() { }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        int cols = area.W >= 80 ? 80 : 40;
        int rows = area.H >= 25 ? 25 : 24;
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
}
