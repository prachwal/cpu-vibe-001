using Cpu.Module;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Modules;

public sealed class ModuleManager
{
    private readonly List<IAppModule> _modules = [];
    private IAppModule? _active;
    private IAppModule _default = null!;
    private readonly Dictionary<ConsoleKey, IAppModule> _keyMap = [];
    private readonly Dictionary<IAppModule, ModuleConfig> _moduleConfig = [];
    private string _statusText = "Ready";

    public IAppModule? Active => _active;
    public string StatusText { get => _statusText; set => _statusText = value; }

    public void Initialize(IEnumerable<IAppModule> modules)
    {
        _modules.Clear();
        _modules.AddRange(modules);

        var config = AppConfig.Load();
        _default = _modules[0];

        foreach (var module in _modules)
        {
            var mc = config.Modules.FirstOrDefault(m =>
                m.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase));

            if (mc != null)
            {
                _moduleConfig[module] = mc;
                if (mc.ConsoleKey.HasValue)
                    _keyMap[mc.ConsoleKey.Value] = module;
                if (mc.Key == null)
                    _default = module;
            }
            else
            {
                _moduleConfig[module] = new ModuleConfig
                {
                    Name = module.Name,
                    TypeName = module.GetType().FullName ?? "",
                    ShowInBar = module.ShowInBar,
                    Label = module.ActivateLabel,
                    Key = module.ActivateKey?.ToString()
                };
                if (module.ActivateKey.HasValue)
                    _keyMap[module.ActivateKey.Value] = module;
                if (module.ActivateKey == null)
                    _default = module;
            }
        }

        _default.OnActivate();
        _active = _default;
    }

    public void SwitchTo(IAppModule module)
    {
        if (_active == module) return;
        _active?.OnDeactivate();
        _active = module;
        module.OnActivate();
    }

    public void SwitchToDefault()
    {
        if (_active != _default)
        {
            _active?.OnDeactivate();
            _default.OnActivate();
            _active = _default;
        }
    }

    public string BuildFunctionBar(int width)
    {
        var parts = _modules
            .Where(m =>
            {
                if (m == _default) return false;
                if (!_moduleConfig.TryGetValue(m, out var mc)) return m.ShowInBar;
                if (!mc.ShowInBar) return false;
                return mc.Key != null;
            })
            .OrderBy(m =>
            {
                if (_moduleConfig.TryGetValue(m, out var mc) && mc.ConsoleKey.HasValue)
                    return (int)mc.ConsoleKey.Value;
                return 999;
            })
            .Select(m =>
            {
                if (_moduleConfig.TryGetValue(m, out var mc) && !string.IsNullOrEmpty(mc.Label))
                    return mc.Label;
                return m.ActivateLabel;
            })
            .ToList();

        if (_active == _default || !_active?.IsTransient == true)
            parts.Add("Esc Quit");

        string bar = string.Join("  ", parts);
        if (width >= bar.Length + _statusText.Length + 4)
            bar += new string(' ', width - bar.Length - _statusText.Length - 2) + $" {_statusText} ";
        return bar.PadRight(width);
    }

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            if (_active != _default) { SwitchToDefault(); return true; }
            return false;
        }

        if (_keyMap.TryGetValue(key.Key, out var target))
        {
            if (_active != target)
            {
                SwitchTo(target);
                return true;
            }
        }

        if (_active != null && _active.OnKey(key))
            return true;

        if (_keyMap.TryGetValue(key.Key, out var toggleTarget) && _active == toggleTarget)
        {
            SwitchToDefault();
            return true;
        }

        return false;
    }

    public bool Tick() => _active?.OnTick() == true;

    public void Render(ITerminalRenderer renderer, int w, int h)
        => (_active ?? _default).OnRender(renderer, w, h);
}
