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
        bool showPanel = w >= 80 + panelW + 4;
        int renderW = showPanel ? w - panelW - 2 : w;

        _view.Render(r, new TermRect(0, 0, renderW, h - 1),
            new PresentationSession(r, new TermRect(0, 0, renderW, h - 1)));

        if (showPanel)
            _view.RenderPanel(r, w, h - 1, panelW);
    }
}
