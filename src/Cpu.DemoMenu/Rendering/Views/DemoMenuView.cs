using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.DemoMenu.Rendering.Views;

public class DemoMenuView : BaseTermView, ITuiSettingsConsumer
{
    private int _selectedIndex;
    public override string Name => "Demo Menu";
    public int SelectedIndex { get => _selectedIndex; set => _selectedIndex = value; }

    public DemoMenuView() { }

    public void ApplySettings(TuiAppSettings settings) { }
    protected override void Seed() { }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        session.Clear();
        string[] names = PiaDemos.Names;
        int top = Math.Max(1, (area.H - names.Length - 2) / 2);

        session.Write(1, top, "PIA Demos", ConsoleColor.Cyan, ConsoleColor.Black);
        top += 2;

        for (int i = 0; i < names.Length; i++)
        {
            bool sel = i == _selectedIndex;
            string line = sel ? $"> {names[i]}" : $"  {names[i]}";
            session.Write(1, top + i, line,
                sel ? ConsoleColor.Black : ConsoleColor.Gray,
                sel ? ConsoleColor.Gray : ConsoleColor.Black);
        }
        session.Write(1, top + names.Length + 1,
            "Enter: run  Esc: back", ConsoleColor.DarkGray, ConsoleColor.Black);
    }
}
