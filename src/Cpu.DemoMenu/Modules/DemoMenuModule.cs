using Cpu.DemoMenu.Rendering.Views;
using Cpu.Module;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.DemoMenu.Modules;

public sealed class DemoMenuModule : ModuleBase
{
    private readonly DemoMenuView _view = new();
    private int _index;

    public override string Name => "Demo Menu";

    public DemoMenuModule(ErrorCollector? errors = null) : base(errors) { }

    protected override void OnActivateCore() { _index = 0; }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        if (Child != null)
        {
            if (Child.OnKey(key))
                return true;
            if (key.Key == ConsoleKey.Escape)
            {
                CloseChild();
                return true;
            }
            return false;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (_index > 0) _index--;
                _view.SelectedIndex = _index;
                return true;
            case ConsoleKey.DownArrow:
                if (_index < PiaDemos.Names.Length - 1) _index++;
                _view.SelectedIndex = _index;
                return true;
            case ConsoleKey.Enter:
                var player = new DemoPlayerModule(_index, Errors);
                SetChild(player);
                player.OnActivate();
                return true;
        }
        return false;
    }

    public override bool OnTick() => Child?.OnTick() ?? false;

    public override void OnRender(ITerminalRenderer r, int w, int h)
    {
        if (Child != null)
        {
            // Child gets full dimensions — it handles its own panel
            Child.OnRender(r, w, h);
            TermArea.Write(r, new TermRect(0, 0, w, h), 1, h - 2, "Esc back to demo list", ConsoleColor.DarkGray, ConsoleColor.Black);
            return;
        }
        base.OnRender(r, w, h);
    }

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        _view.SelectedIndex = _index;
        _view.Render(r, new TermRect(x, y, w, h),
            new PresentationSession(r, new TermRect(x, y, w, h)));
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y) { }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        if (Child != null) return; // player handles its own panel

        y++; PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan); y++;
        PanelLine(r, w, y++, " Up/Dn select");
        PanelLine(r, w, y++, " Enter run");
    }
}
