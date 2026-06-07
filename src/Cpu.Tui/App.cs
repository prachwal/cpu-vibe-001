using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public partial class App
{
    private readonly ITerminalRenderer _renderer;
    private readonly ModuleManager _modules;
    private readonly ITuiAppConfiguration _config;
    private bool _running;
    private int _lastW, _lastH;
    private bool _dirty = true;
    private bool _fullRedraw = true;
    private bool _mouseEnabled;

    public App(ITerminalRenderer renderer, ModuleManager modules, ITuiAppConfiguration config)
    {
        _renderer = renderer;
        _modules = modules;
        _config = config;
        _config.Changed += OnConfigChanged;
    }

    private void OnConfigChanged()
    {
        _dirty = true;
        _fullRedraw = true;
    }

    private void RenderTooSmall(int w, int h)
    {
        var a = new TermRect(0, 0, w, h - 1);
        TermArea.Write(_renderer, a, 0, 0, "Terminal too small", ConsoleColor.White, ConsoleColor.DarkRed);
        TermArea.Write(_renderer, a, 0, 1, $"Current: {w}x{h}", ConsoleColor.Gray, ConsoleColor.Black);
        TermArea.Write(_renderer, a, 0, 2, "Minimum: 40x25", ConsoleColor.Gray, ConsoleColor.Black);
        TermArea.Write(_renderer, a, 0, Math.Max(0, a.H - 1), "Esc Quit", ConsoleColor.Black, ConsoleColor.Gray);
    }
}
