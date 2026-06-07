using Cpu.DemoMenu.Rendering.Views;
using Cpu.Module;
using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.DemoMenu.Modules;

public sealed class DemoMenuModule : IAppModule
{
    private readonly PiaDevice _pia;
    private readonly ScreenBuffer _screen;
    private readonly DemoMenuView _view = new();
    private PiaTerminalAdapter? _terminal;
    private int _index;
    private bool _running;
    private bool _paused;
    private int _speed = 1;
    private string[] _buffer = [];
    private int _line, _ch;

    public string Name => "Demo Menu";
    public ConsoleKey? ActivateKey => ConsoleKey.F4;
    public string ActivateLabel => "F4 Demo";
    public bool IsTransient => false;
    public bool ShowInBar => true;
    public bool IsActive { get; private set; }

    public DemoMenuModule(ScreenBuffer screen)
    {
        _pia = new PiaDevice(0x8800);
        _screen = screen;
        _terminal = new PiaTerminalAdapter(_pia, _screen);
    }

    public void OnActivate() { IsActive = true; _running = false; _index = 0; }
    public void OnDeactivate() { IsActive = false; _running = false; }

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (_running)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _running = false;
                    _view.SelectedIndex = _index;
                    return true;
                case ConsoleKey.P:
                    _paused = !_paused;
                    return true;
                case ConsoleKey.N:
                    _index = (_index + 1) % PiaDemos.Names.Length;
                    Start();
                    return true;
                case ConsoleKey.OemPlus:
                case ConsoleKey.Add:
                    _speed = Math.Min(10, _speed + 1);
                    return true;
                case ConsoleKey.OemMinus:
                case ConsoleKey.Subtract:
                    _speed = Math.Max(1, _speed - 1);
                    return true;
            }
            return true;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (_index > 0) _index--;
                _view.SelectedIndex = _index;
                return true;
            case ConsoleKey.DownArrow:
                if (_index < PiaDemos.Names.Length - 1) _index++;
                _view.SelectedIndex = _index;
                return true;
            case ConsoleKey.Enter:
                Start();
                return true;
        }
        return false;
    }

    private void Start()
    {
        _screen.Reset(); _pia.Reset();
        var a = _pia.BaseAddress;
        _pia.Write(a, 0xFF);
        _pia.Write((ushort)(a + 2), 0xFF);
        _pia.Write((ushort)(a + 1), 0x04);
        _pia.Write((ushort)(a + 3), 0x04);
        _pia.Write(a, 0x00);
        _pia.Write((ushort)(a + 2), 0x00);
        _buffer = PiaDemos.GetText(_index).Split('\n');
        _line = 0; _ch = 0; _running = true; _paused = false;
    }

    public bool OnTick()
    {
        if (!_running || _paused) return false;
        for (int s = 0; s < _speed; s++)
        {
            if (_line >= _buffer.Length) { _running = false; return true; }
            if (_ch < _buffer[_line].Length)
            {
                byte b = (byte)_buffer[_line][_ch++];
                _pia.Write(_pia.BaseAddress, b);
                _pia.Write((ushort)(_pia.BaseAddress + 2), 0x08);
                _pia.Write((ushort)(_pia.BaseAddress + 2), 0x00);
            }
            else
            {
                _pia.Write(_pia.BaseAddress, 0x0D);
                _pia.Write((ushort)(_pia.BaseAddress + 2), 0x08);
                _pia.Write((ushort)(_pia.BaseAddress + 2), 0x00);
                _pia.Write(_pia.BaseAddress, 0x0A);
                _pia.Write((ushort)(_pia.BaseAddress + 2), 0x08);
                _pia.Write((ushort)(_pia.BaseAddress + 2), 0x00);
                _line++; _ch = 0;
            }
        }
        return true;
    }

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        if (_running)
        {
            var s = new PresentationSession(r, new TermRect(0, 0, w, h - 1));
            var layout = TerminalLayout.From(w, h);
            var sz = layout.Fit(new ScreenSize(25, 80));
            var f = s.CenterFrame(sz.Cols, sz.Rows);
            s.Clear();
            s.DrawFrame(f, FrameStyle.Ascii);
            s.RenderScreen(_screen, sz.Rows, sz.Cols, f.Inner);
            string status = _paused ? "PAUSED" : $"{_speed}x";
            string bar = $"Esc Back  P {(_paused ? "Resume" : "Pause")}  N Next  + Slower  - Faster  [{status}]";
            TermArea.Write(r, new TermRect(0, 0, w, h - 1), 1, h - 2, bar, ConsoleColor.DarkGray, ConsoleColor.Black);
        }
        else
        {
            _view.SelectedIndex = _index;
            _view.Render(r, new TermRect(0, 0, w, h - 1),
                new PresentationSession(r, new TermRect(0, 0, w, h - 1)));
        }
    }
}
