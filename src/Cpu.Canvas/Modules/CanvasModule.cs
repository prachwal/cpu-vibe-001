using Cpu.Canvas.Rendering.Views;
using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Canvas.Modules;

public sealed class CanvasModule : ModuleBase
{
    private readonly CanvasView _view = new();
    private static readonly string[] DemoLabels = ["Gradient Mandala", "ZX Spectrum", "3D Shapes"];
    public override string Name => "Canvas";

    public CanvasModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }

    protected override void OnActivateCore() => _view.Mode = Config.Current.DefaultGraphicsMode;

    public override bool OnTick() { _view.Tick3D(); return true; }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow: _view.PrevDemo(); return true;
            case ConsoleKey.RightArrow: _view.NextDemo(); return true;
            case ConsoleKey.F10: _view.CycleMode(); return true;
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.W:
            case ConsoleKey.A:
            case ConsoleKey.S:
            case ConsoleKey.D:
                _view.Handle3DKey(key.Key); return true;
        }
        return false;
    }

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        SyncViewSettings(_view);
        _view.Render(r, new TermRect(x, y, w, h),
            new PresentationSession(r, new TermRect(x, y, w, h)));
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y)
    {
        PanelLine(r, w, y++, $" Demo: {DemoLabels[_view.DemoIndex]}");
        PanelLine(r, w, y++, $" Mode: {_view.Mode}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++; PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan); y++;
        PanelLine(r, w, y++, " <-  prev demo");
        PanelLine(r, w, y++, " ->  next demo");
        PanelLine(r, w, y++, " F10 cycle mode");
        if (_view.DemoIndex == 2)
        {
            PanelLine(r, w, y++, " WASD rotate");
            PanelLine(r, w, y++, " Up/Dn zoom");
        }
    }
}
