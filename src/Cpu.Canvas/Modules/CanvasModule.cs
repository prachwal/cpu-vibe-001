using Cpu.Canvas.Rendering.Views;
using Cpu.Module;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;

namespace Cpu.Canvas.Modules;

public sealed class CanvasModule : IAppModule
{
    private readonly CanvasView _view = new();

    public string Name => "Canvas";
    public ConsoleKey? ActivateKey => ConsoleKey.F9;
    public string ActivateLabel => "F9 Canvas";
    public bool IsTransient => false;
    public bool ShowInBar => true;
    public bool IsActive { get; private set; }

    public void OnActivate() { IsActive = true; }
    public void OnDeactivate() { IsActive = false; }
    public bool OnTick() { _view.Tick3D(); return true; }

    public bool OnKey(ConsoleKeyInfo key)
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

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        int panelW = 30;
        bool showPanel = w >= 80 + panelW + 4;
        int renderW = showPanel ? w - panelW - 2 : w;

        _view.Render(r, new TermRect(0, 0, renderW, h - 1),
            new PresentationSession(r, new TermRect(0, 0, renderW, h - 1)));

        if (showPanel)
            RenderPanel(r, w, h - 1, panelW);

        string controls = _view.DemoIndex == 2
            ? "WASD/Arrows Rotate  Up/Down Zoom  Left/Right Demo  F10 Mode"
            : "Left/Right Demo  F10 Mode";
        var term = new TermRect(0, 0, w, h - 1);
        TermArea.Write(r, term, 1, h - 2, controls, ConsoleColor.DarkGray, ConsoleColor.Black);
    }

    private static readonly string[] DemoLabels = ["Gradient Mandala", "ZX Spectrum", "3D Shapes"];

    private void RenderPanel(ITerminalRenderer r, int w, int h, int panelW)
    {
        int x = w - panelW;
        var bg = ConsoleColor.DarkBlue;
        var fg = ConsoleColor.Gray;

        for (int i = 0; i < h; i++)
            for (int c = 0; c < panelW; c++)
                r.SetCell(x + c, i, ' ', fg, bg);

        int y = 1;
        PanelLine(r, x, y++, panelW, " Canvas", ConsoleColor.Cyan, bg);
        y++;
        PanelLine(r, x, y++, panelW, $" Demo: {DemoLabels[_view.DemoIndex]}", fg, bg);
        PanelLine(r, x, y++, panelW, $" Mode: {_view.Mode}", fg, bg);
        y++;
        PanelLine(r, x, y++, panelW, " Controls", ConsoleColor.Cyan, bg);
        y++;
        PanelLine(r, x, y++, panelW, " <-  prev demo", fg, bg);
        PanelLine(r, x, y++, panelW, " ->  next demo", fg, bg);
        PanelLine(r, x, y++, panelW, " F10 cycle mode", fg, bg);
        if (_view.DemoIndex == 2)
        {
            PanelLine(r, x, y++, panelW, " WASD rotate", fg, bg);
            PanelLine(r, x, y++, panelW, " Up/Dn zoom", fg, bg);
        }
    }

    private static void PanelLine(ITerminalRenderer r, int x, int y, int w, string text, ConsoleColor fg, ConsoleColor bg)
    {
        int max = w;
        if (text.Length > max) text = text[..max];
        if (text.Length < max) text += new string(' ', max - text.Length);
        TermArea.Write(r, new TermRect(0, 0, w, 100), x, y, text, fg, bg);
    }
}
