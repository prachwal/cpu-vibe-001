using Cpu.Image.Rendering.Views;
using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Image.Modules;

public sealed class ImageModule : ModuleBase
{
    private readonly ImageView _view = new();
    private int _index;
    public override string Name => "Image";

    public ImageModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }

    protected override void OnActivateCore()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "samples");
        _view.Paths = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.jpg").OrderBy(f => f).ToArray()
            : [];
        _index = 0;
        _view.Index = 0;
        _view.Mode = Config.Current.DefaultGraphicsMode;
    }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.F7:
                _index = (_index + 1) % Math.Max(1, _view.Paths.Length);
                _view.Index = _index; return true;
            case ConsoleKey.LeftArrow:
                if (_view.Paths.Length > 0)
                    _index = (_index + _view.Paths.Length - 1) % _view.Paths.Length;
                _view.Index = _index; return true;
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.F8:
                _view.CycleMode(); return true;
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
        string name = _view.Paths.Length > 0 ? Path.GetFileName(_view.Paths[_index]) : "(none)";
        string mode = _view.Mode.ToString();
        string idx = _view.Paths.Length > 0 ? $"{_index + 1}/{_view.Paths.Length}" : "0/0";
        PanelLine(r, w, y++, $" File: {name}");
        PanelLine(r, w, y++, $" Mode: {mode}");
        PanelLine(r, w, y++, $" Idx:  {idx}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++; PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan); y++;
        PanelLine(r, w, y++, " <-  prev image");
        PanelLine(r, w, y++, " ->  next image");
        PanelLine(r, w, y++, " Up  cycle mode");
        PanelLine(r, w, y++, " Dn  cycle mode");
    }
}
