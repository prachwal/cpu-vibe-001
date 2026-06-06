using Cpu.Module;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Tui.Modules;

public sealed class ImageModule : IAppModule
{
    private readonly ImageView _view = new();
    private string[] _paths = [];
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    public string Name => "Image";
    public ConsoleKey? ActivateKey => ConsoleKey.F7;
    public string ActivateLabel => "F7 Image";
    public bool IsTransient => false;
    public bool ShowInBar => true;
    public bool IsActive { get; private set; }

    public void OnActivate()
    {
        IsActive = true;
        _paths = Directory.Exists("samples")
            ? Directory.GetFiles("samples", "*.jpg").OrderBy(p => p).ToArray()
            : [];
        _view.Paths = _paths; _view.Index = 0; _view.Mode = _mode;
    }

    public void OnDeactivate() { IsActive = false; }
    public bool OnTick() => false;

    public bool OnKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.F7: _view.NextImage(); _mode = _view.Mode; return true;
            case ConsoleKey.LeftArrow: _view.PrevImage(); _mode = _view.Mode; return true;
            case ConsoleKey.F8: _view.CycleMode(); _mode = _view.Mode; return true;
        }
        return false;
    }

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        _view.Paths = _paths; _view.Mode = _mode;
        _view.Render(r, new TermRect(0, 0, w, h - 1));
    }
}
