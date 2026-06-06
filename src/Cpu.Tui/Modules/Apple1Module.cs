using Cpu.Board.Core;
using Cpu.Board.Core.Adapters;
using Cpu.Module;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Tui.Modules;

public sealed class Apple1Module : IAppModule
{
    private Apple1View? _view;
    private MachineBoard? _board;
    private bool _started;
    private bool _activated;

    private readonly string[] _profiles = ["apple-1.json", "apple-1-basic.json"];
    private readonly string[] _profileNames = ["Woz Monitor", "BASIC"];
    private int _profileIndex;

    public string Name => "Apple 1";
    public ConsoleKey? ActivateKey => ConsoleKey.F8;
    public string ActivateLabel => "F8 Apple1";
    public bool IsTransient => false;
    public bool ShowInBar => true;
    public bool IsActive { get; private set; }

    public Apple1Module() { }

    public void OnActivate()
    {
        IsActive = true;
        LoadProfile(_profiles[_profileIndex]);
    }

    public void OnDeactivate()
    {
        IsActive = false; _view = null; _started = false; _activated = false;
        _board?.Dispose(); _board = null;
    }

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.UpArrow)
        {
            _profileIndex = (_profileIndex - 1 + _profiles.Length) % _profiles.Length;
            _board?.Dispose();
            LoadProfile(_profiles[_profileIndex]);
            return true;
        }
        if (key.Key == ConsoleKey.DownArrow)
        {
            _profileIndex = (_profileIndex + 1) % _profiles.Length;
            _board?.Dispose();
            LoadProfile(_profiles[_profileIndex]);
            return true;
        }
        if (key.Key == ConsoleKey.F10)
        {
            _profileIndex = (_profileIndex + 1) % _profiles.Length;
            _board?.Dispose();
            LoadProfile(_profiles[_profileIndex]);
            return true;
        }
        if (_view != null)
        {
            if (key.Key == ConsoleKey.Enter) _view.EnqueueKey('\r');
            else if (key.Key == ConsoleKey.Backspace) _view.EnqueueKey('\b');
            else if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F) _view.EnqueueKey(key.KeyChar);
        }
        return true;
    }

    public bool OnTick()
    {
        if (!_started || !_activated || _view == null) return false;
        _view.StepCpu(5000);
        return true;
    }

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        if (_view == null) return;
        var area = new TermRect(0, 0, w, h - 1);
        if (!_activated)
        {
            _view.Activate(r, area);
            if (_profileIndex == 1)
                _view.EnqueueText("E000R\r");
            _activated = true;
        }
        _view.Render(r, area);
    }

    private void LoadProfile(string profileFile)
    {
        try
        {
            string p = Path.Combine(AppContext.BaseDirectory, "profiles", profileFile);
            if (!File.Exists(p)) return;

            var profile = MachineBoard.LoadProfile(p);
            _board = new MachineBoard(profile);
            var pia = new PiaDevice(profile.Pia!.BaseAddress);
            _board.AttachDevice(pia);

            var display = new Apple1DisplayAdapter(
                profile.Display?.Cols ?? 40, profile.Display?.Rows ?? 24);
            var keyboard = new Apple1KeyboardAdapter();

            _view = new Apple1View(_board, pia, display, keyboard,
                _profileNames[_profileIndex], _profileNames, _profileIndex);

            _activated = false;
            _started = true;
        }
        catch { _view = null; _started = false; _activated = false; }
    }
}
