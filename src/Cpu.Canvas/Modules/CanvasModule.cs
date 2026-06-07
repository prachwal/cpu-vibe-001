using Cpu.Canvas.Rendering.Views;
using Cpu.Module;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;

namespace Cpu.Canvas.Modules;

public sealed class CanvasModule : IAppModule
{
    private readonly CanvasView _view = new();
    private static readonly string[] DemoLabels = ["Gradient Mandala", "ZX Spectrum", "3D Shapes"];

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
        bool showPanel = w >= panelW + 80 + 4;
        int renderX = showPanel ? panelW + 2 : 0;
        int renderW = showPanel ? w - panelW - 2 : w;

        _view.Render(r, new TermRect(renderX, 0, renderW, h - 1),
            new PresentationSession(r, new TermRect(renderX, 0, renderW, h - 1)));

        if (showPanel)
            RenderPanel(r, h - 1, panelW);
    }

    private void RenderPanel(ITerminalRenderer r, int h, int panelW)
    {
        var bg = ConsoleColor.DarkBlue;
        var fg = ConsoleColor.Gray;
        for (int i = 0; i < h; i++)
            for (int c = 0; c < panelW; c++)
                r.SetCell(c, i, ' ', fg, bg);

        string mode = _view.Mode.ToString();
        int y = 1;
        PLine(r, panelW, y++, "  Info", ConsoleColor.Cyan, bg);
        y++;
        PLine(r, panelW, y++, $" Demo: {DemoLabels[_view.DemoIndex]}", fg, bg);
        PLine(r, panelW, y++, $" Mode: {mode}", fg, bg);
        y++;
        PLine(r, panelW, y++, "  Controls", ConsoleColor.Cyan, bg);
        y++;
        PLine(r, panelW, y++, " <-  prev demo", fg, bg);
        PLine(r, panelW, y++, " ->  next demo", fg, bg);
        PLine(r, panelW, y++, " F10 cycle mode", fg, bg);
        if (_view.DemoIndex == 2)
        {
            PLine(r, panelW, y++, " WASD rotate", fg, bg);
            PLine(r, panelW, y++, " Up/Dn zoom", fg, bg);
        }
    }

    private static void PLine(ITerminalRenderer r, int w, int y, string text, ConsoleColor fg, ConsoleColor bg)
    {
        int max = w;
        if (text.Length > max) text = text[..max];
        if (text.Length < max) text += new string(' ', max - text.Length);
        TermArea.Write(r, new TermRect(0, 0, w, 100), 0, y, text, fg, bg);
    }
}
