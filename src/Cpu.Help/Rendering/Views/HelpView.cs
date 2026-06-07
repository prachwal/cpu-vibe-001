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
            "Global:",
            " F1 Help   F4 Demo   F7 Image",
            " F8 Apple1 F9 Canvas  Esc back/quit",
            "",
            "Screen:",
            " F5 cycle size  F6 echo  F10 frame UTF",
            "",
            "Canvas: <-/-> demo  F10 graphics mode",
            "Image:  <-/-> file  Up/Dn/F8 graphics",
            "Demo:   P pause  +/- speed  Esc list"
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
