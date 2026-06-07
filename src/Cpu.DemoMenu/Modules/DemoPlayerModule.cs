using Cpu.Module;
using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Layout;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.DemoMenu.Modules;

public sealed class DemoPlayerModule : ModuleBase
{
    private readonly PiaDevice _pia;
    private readonly ScreenBuffer _screen;
    private readonly PiaTerminalAdapter _terminal;
    private bool _paused;
    private int _speed = 1;
    private int _line, _ch;
    private string[] _buffer = [];
    private bool _ended;
    private readonly string _name;

    public override string Name => _name;

    public DemoPlayerModule(int demoIndex, ErrorCollector? errors = null) : base(errors)
    {
        _name = PiaDemos.Names[demoIndex];
        _pia = new PiaDevice(0x8800);
        _screen = new ScreenBuffer();
        _terminal = new PiaTerminalAdapter(_pia, _screen);
        _buffer = PiaDemos.GetText(demoIndex).Split('\n');
    }

    protected override void OnActivateCore()
    {
        _screen.Reset(); _pia.Reset();
        var a = _pia.BaseAddress;
        _pia.Write(a, 0xFF); _pia.Write((ushort)(a + 2), 0xFF);
        _pia.Write((ushort)(a + 1), 0x04); _pia.Write((ushort)(a + 3), 0x04);
        _pia.Write(a, 0x00); _pia.Write((ushort)(a + 2), 0x00);
        _line = 0; _ch = 0; _paused = false; _ended = false;
    }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        if (_ended)
        {
            if (key.Key == ConsoleKey.Escape) return false; // fall through to parent
            return true;
        }

        switch (key.Key)
        {
            case ConsoleKey.P: _paused = !_paused; return true;
            case ConsoleKey.OemPlus:
            case ConsoleKey.Add: _speed = Math.Min(10, _speed + 1); return true;
            case ConsoleKey.OemMinus:
            case ConsoleKey.Subtract: _speed = Math.Max(1, _speed - 1); return true;
        }
        return true;
    }

    public override bool OnTick()
    {
        if (_ended || _paused) return false;

        for (int s = 0; s < _speed; s++)
        {
            if (_line >= _buffer.Length) { _ended = true; return true; }
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

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        var s = new PresentationSession(r, new TermRect(x, y, w, h));
        var layout = TerminalLayout.From(w, h);
        var sz = layout.Fit(new ScreenSize(25, 80));
        var f = s.CenterFrame(sz.Cols, sz.Rows);
        s.Clear();
        s.DrawFrame(f, FrameStyle.Ascii, _name);
        s.RenderScreen(_screen, sz.Rows, sz.Cols, f.Inner);

        if (_ended)
            s.Write(1, y + h - 1, "Demo finished. Esc to return.", ConsoleColor.DarkGray, ConsoleColor.Black);
        else
        {
            string status = _paused ? "PAUSED" : $"{_speed}x";
            string bar = $"P {(_paused ? "Resume" : "Pause")}  + {_speed+1}x  - {_speed-1}x  [{status}]";
            TermArea.Write(r, new TermRect(0, 0, w, h), x + 1, y + h - 1, bar, ConsoleColor.DarkGray, ConsoleColor.Black);
        }
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y)
    {
        PanelLine(r, w, y++, $" Demo: {_name}");
        PanelLine(r, w, y++, $" Speed: {_speed}x");
        PanelLine(r, w, y++, $" Status: {(_ended ? "Done" : _paused ? "Paused" : "Playing")}");
        PanelLine(r, w, y++, $" Lines: {_line}/{_buffer.Length}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++; PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan); y++;
        if (_ended)
        {
            PanelLine(r, w, y++, " Esc back");
        }
        else
        {
            PanelLine(r, w, y++, " P pause/resume");
            PanelLine(r, w, y++, " +  speed up");
            PanelLine(r, w, y++, " -  speed down");
        }
    }
}
