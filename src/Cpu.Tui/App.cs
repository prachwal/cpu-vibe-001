using Cpu.Module;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public partial class App
{
    private readonly ITerminalRenderer _renderer;
    private readonly ModuleManager _modules;
    private bool _running;
    private int _lastW, _lastH;
    private bool _dirty = true;
    private bool _fullRedraw = true;

    public App(ITerminalRenderer renderer, ModuleManager modules, IEnumerable<IAppModule> allModules)
    {
        _renderer = renderer;
        _modules = modules;
        _modules.Initialize(allModules);
    }

    public void Run()
    {
        Console.TreatControlCAsInput = true;
        _running = true;

        try
        {
            while (_running)
            {
                int w = Console.WindowWidth, h = Console.WindowHeight;
                bool changed = w != _lastW || h != _lastH;
                if (changed) _renderer.Resize(w, h);

                if (_dirty || changed)
                {
                    Render(w, h, changed || _fullRedraw);
                    _lastW = w; _lastH = h;
                    _dirty = false; _fullRedraw = false;
                }

                ReadInput();
                if (_modules.Tick())
                    _dirty = true;
                Thread.Sleep(20);
            }
        }
        finally
        {
            _renderer.Dispose();
            Console.CursorVisible = true;
            Console.Clear();
        }
    }

    private void Render(int w, int h, bool fullRedraw)
    {
        if (fullRedraw) _renderer.Clear(ConsoleColor.Black, ConsoleColor.Black);
        if (w < 40 || h < 25) { RenderTooSmall(w, h); _renderer.Flush(); return; }

        _modules.Render(_renderer, w, h);
        RenderFunctionBar(w, h);
        _renderer.Flush();
    }

    private void RenderTooSmall(int w, int h)
    {
        var a = new TermRect(0, 0, w, h - 1);
        TermArea.Write(_renderer, a, 0, 0, "Terminal too small", ConsoleColor.White, ConsoleColor.DarkRed);
        TermArea.Write(_renderer, a, 0, 1, $"Current: {w}x{h}", ConsoleColor.Gray, ConsoleColor.Black);
        TermArea.Write(_renderer, a, 0, 2, "Minimum: 40x25", ConsoleColor.Gray, ConsoleColor.Black);
        TermArea.Write(_renderer, a, 0, Math.Max(0, a.H - 1), "Esc Quit", ConsoleColor.Black, ConsoleColor.Gray);
    }

    private void ReadInput()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape && _modules.Active?.ActivateKey == null)
            { _running = false; return; }
            if (_modules.OnKey(key))
            {
                _dirty = true;
                _fullRedraw = true;
            }
        }
    }

    private void RenderFunctionBar(int w, int h)
    {
        string bar = _modules.BuildFunctionBar(w);
        TermArea.Write(_renderer, new TermRect(0, 0, w, h), 0, h - 1, bar, ConsoleColor.Black, ConsoleColor.Gray);
    }
}
