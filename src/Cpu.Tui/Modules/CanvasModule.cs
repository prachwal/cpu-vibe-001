using Cpu.Module;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Tui.Modules;

public sealed class CanvasModule : IAppModule
{
    private readonly CanvasView _view = new();
    private int _lastW, _lastH;
    public string Name => "Canvas";
    public ConsoleKey? ActivateKey => ConsoleKey.F9;
    public string ActivateLabel => "F9 Canvas";
    public bool IsTransient => false;
    public bool ShowInBar => true;
    public bool IsActive { get; private set; }

    public CanvasModule() { }

    public void OnActivate()
    {
        IsActive = true;
        _view.ResetContent();
        _view.RequireFullClear();
    }

    public void OnDeactivate() { IsActive = false; }

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (_view.DemoIndex == 2 && (key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow
            or ConsoleKey.W or ConsoleKey.A or ConsoleKey.S or ConsoleKey.D))
        { _view.Handle3DKey(key.Key); return true; }
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow: _view.PrevDemo(); return true;
            case ConsoleKey.RightArrow: _view.NextDemo(); return true;
            case ConsoleKey.F10: _view.CycleMode(); return true;
        }
        return false;
    }

    public bool OnTick()
    {
        if (_view.DemoIndex != 2) return false;
        _view.Tick3D();
        return true;
    }

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        _lastW = w; _lastH = h;
        int panelW = w >= 66 ? 26 : 0;
        var ct = new TermRect(0, 0, w - panelW, h - 1);
        _view.Render(r, ct);
        if (panelW == 0) return;

        var a = new TermRect(w - panelW, 0, panelW, h - 1);
        var s = new PresentationSession(r, a);
        s.Clear();
        var f = s.CenterFrame(panelW - 2, h - 3);
        var inner = s.DrawFrame(f, FrameStyle.Ascii, "Info");
        var buf = _view.Canvas.Buffer;
        var mode = _view.Mode;
        int mc = Math.Max(1, ct.W - 4), mr = Math.Max(1, ct.H - 4);
        var (cols, rows) = PresentationSession.FitImage(buf, mc, mr, mode);
        int y = 1;
        PL(r, inner, 1, y++, $"Canvas: {buf.Width}x{buf.Height}");
        y++;
        PL(r, inner, 1, y++, $"Cells: {cols}x{rows}");
        PL(r, inner, 1, y++, $"Mode: {mode}");
        y++;
        PL(r, inner, 1, y++, $"Term: {w}x{h - 1}");
    }

    private static void PL(ITerminalRenderer r, TermRect a, int x, int y, string t)
    {
        int m = a.W - x; if (t.Length > m) t = t[..m];
        if (t.Length > 0) TermArea.Write(r, a, x, y, t, ConsoleColor.Gray, ConsoleColor.Black);
    }
}
