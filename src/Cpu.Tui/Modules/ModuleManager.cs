using Cpu.Module;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Modules;

public sealed class ModuleManager
{
    private readonly MainMenuModule _root;
    private IAppModule _active;

    public IAppModule Root => _root;
    public IAppModule Active => _active;

    public ModuleManager(MainMenuModule root)
    {
        _root = root;
        _active = root;
    }

    public void UpdateActive()
    {
        _active = _root;
        while (_active.Child != null)
            _active = _active.Child;
    }

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (_root.OnKey(key))
        {
            UpdateActive();
            return true;
        }
        return false;
    }

    public bool OnMouse(MouseEvent e)
    {
        for (var m = _active; m != null; m = m.Parent)
            if (m.OnMouse(e)) return true;
        return false;
    }

    public bool CurrentWantsMouse
    {
        get
        {
            for (var m = _active; m != null; m = m.Parent)
                if (!m.WantsMouse) return false;
            return true;
        }
    }

    public bool Tick()
    {
        bool result = false;
        for (var m = _active; m != null; m = m.Parent)
            result |= m.OnTick();
        return result;
    }

    public void Render(ITerminalRenderer renderer, int w, int h)
        => _root.OnRender(renderer, w, h);
}
