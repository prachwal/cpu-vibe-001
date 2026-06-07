using Cpu.Tui;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Help.Rendering.Views;

public class HelpView : BaseTermView, ITuiSettingsConsumer
{
    public override string Name => "Help";

    public void ApplySettings(TuiAppSettings settings) { }

    protected override void Seed() { }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        session.Clear();
        string[] lines =
        [
            "CPU-VIBE terminal", "",
            "Global:",
            " F1 Help   F2 Setup  F4 Demo",
            " F7 Image  F8 Apple1 F9 Canvas",
            " Esc back/quit",
            "",
            "Setup: theme, frames, panel, mouse",
            "Menu/Setup: click when mouse On",
            "Screen: F5 size  F6 echo",
            "Canvas: <-/-> demo  F8/F10 graphics",
            "        demo 4 = Mandelbrot",
            "Image:  <-/-> file  F8/F10 graphics",
            "Demo:   P pause  +/- speed"
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
