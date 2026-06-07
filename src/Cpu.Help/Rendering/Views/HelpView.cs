using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Help.Rendering.Views;

public class HelpView : BaseTermView
{
    public override string Name => "Help";
    protected override void Seed() { }
    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        session.Clear();
        string[] lines =
        [
            "CPU-VIBE terminal", "",
            "F1  Help  F2  Cycle screen", "F3  Echo mode  F4  Demo menu",
            "F5  Refresh  F6  Frame style", "F7  Image viewer  F9  Canvas",
            "Esc Quit", "",
            "Echo: type text, arrows move cursor.", "Canvas: left/right switch demo, F10 cycle mode"
        ];
        session.Centered(lines, ConsoleColor.Gray, ConsoleColor.Black);
        if (lines.Length > 1)
        {
            int top = Math.Max(0, (area.H - lines.Length) / 2);
            int left = Math.Max(0, (area.W - lines[0].Length) / 2);
            TermArea.Write(r, area, left, top, lines[0], ConsoleColor.Cyan, ConsoleColor.Black);
        }
    }
}
