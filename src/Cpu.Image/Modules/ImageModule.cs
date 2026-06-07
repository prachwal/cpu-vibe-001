using Cpu.Image.Rendering.Views;
using Cpu.Module;
using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;

namespace Cpu.Image.Modules;

public sealed class ImageModule : IAppModule
{
    private readonly ImageView _view = new();
    private int _index;

    public string Name => "Image";
    public ConsoleKey? ActivateKey => ConsoleKey.F7;
    public string ActivateLabel => "F7 Image";
    public bool IsTransient => false;
    public bool ShowInBar => true;
    public bool IsActive { get; private set; }

    public void OnActivate()
    {
        IsActive = true;
        string path = Path.Combine(AppContext.BaseDirectory, "samples");
        _view.Paths = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.jpg").OrderBy(f => f).ToArray()
            : [];
        _index = 0;
        _view.Index = 0;
    }

    public void OnDeactivate() { IsActive = false; }
    public bool OnTick() => false;

    public bool OnKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.F7:
                _index = (_index + 1) % Math.Max(1, _view.Paths.Length);
                _view.Index = _index;
                return true;
            case ConsoleKey.LeftArrow:
                if (_view.Paths.Length > 0)
                    _index = (_index + _view.Paths.Length - 1) % _view.Paths.Length;
                _view.Index = _index;
                return true;
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.F8:
                _view.CycleMode();
                return true;
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
            RenderPanel(r, w, h - 1, panelW);
    }

    private void RenderPanel(ITerminalRenderer r, int w, int h, int panelW)
    {
        var bg = ConsoleColor.DarkBlue;
        var fg = ConsoleColor.Gray;
        for (int i = 0; i < h; i++)
            for (int c = 0; c < panelW; c++)
                r.SetCell(c, i, ' ', fg, bg);

        string name = _view.Paths.Length > 0 ? Path.GetFileName(_view.Paths[_index]) : "(none)";
        string mode = _view.Mode.ToString();
        string idx = _view.Paths.Length > 0 ? $"{_index + 1}/{_view.Paths.Length}" : "0/0";

        int y = 1;
        PLine(r, panelW, y++, "  Info", ConsoleColor.Cyan, bg);
        y++;
        PLine(r, panelW, y++, $" File: {name}", fg, bg);
        PLine(r, panelW, y++, $" Mode: {mode}", fg, bg);
        PLine(r, panelW, y++, $" Idx:  {idx}", fg, bg);
        y++;
        PLine(r, panelW, y++, "  Controls", ConsoleColor.Cyan, bg);
        y++;
        PLine(r, panelW, y++, " <-  prev image", fg, bg);
        PLine(r, panelW, y++, " ->  next image", fg, bg);
        PLine(r, panelW, y++, " Up  cycle mode", fg, bg);
        PLine(r, panelW, y++, " Dn  cycle mode", fg, bg);
    }

    private static void PLine(ITerminalRenderer r, int w, int y, string text, ConsoleColor fg, ConsoleColor bg)
    {
        int max = w;
        if (text.Length > max) text = text[..max];
        if (text.Length < max) text += new string(' ', max - text.Length);
        TermArea.Write(r, new TermRect(0, 0, w, 100), 0, y, text, fg, bg);
    }
}
