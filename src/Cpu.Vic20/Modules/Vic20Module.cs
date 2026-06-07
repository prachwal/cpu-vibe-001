using Cpu.Vic20.Devices;
using Cpu.Vic20.Rendering.Views;
using Cpu.Vic20.System;
using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Vic20.Modules;

public sealed class Vic20Module : ModuleBase
{
    private const int MinWidthForKeyPanel = PanelWidth + PanelWidth + 50;

    private Vic20View? _view;
    private Vic20Machine? _machine;
    private bool _activated;
    private string? _loadError;

    public override string Name => "Commodore VIC-20";
    public override bool WantsMouse => false;

    public Vic20Module(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }

    protected override int SecondaryPanelWidth => PanelWidth;

    protected override int SecondaryPanelMinWidth => MinWidthForKeyPanel;

    protected override void OnActivateCore()
    {
        LoadMachine();
        if (_view != null)
        {
            _view.DisplayMode = Vic20DisplayMode.Text;
            _view.GraphicsMode = Config.Current.DefaultGraphicsMode;
        }
    }

    protected override void OnDeactivateCore()
    {
        ReleaseAllKeys();
        _view = null;
        _activated = false;
        _loadError = null;
        _machine?.Dispose();
        _machine = null;
    }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.F1:
            case ConsoleKey.F2:
            case ConsoleKey.F4:
            case ConsoleKey.F7:
            case ConsoleKey.F8:
            case ConsoleKey.F9:
            case ConsoleKey.F11:
            case ConsoleKey.Escape:
                return false;
        }

        if (_view == null)
            return false;

        if (key.Key == ConsoleKey.F10)
        {
            _view.ToggleDisplayMode();
            return true;
        }

        if (key.Key == ConsoleKey.F12)
        {
            _view.ReleaseAllHeldKeys();
            return true;
        }

        if (VicHostKeyMap.TryMapConsoleKey(key, out int row, out int col))
        {
            _view.TapKey(row, col);
            return true;
        }

        if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F
            && VicHostKeyMap.TryMapHostChar(key.KeyChar, out row, out col))
        {
            _view.TapKey(row, col);
            return true;
        }

        return false;
    }

    public override bool OnTick()
    {
        if (!IsActive || _view == null)
            return false;
        _view.StepCpu();
        return true;
    }

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        if (_view == null)
            return;

        SyncViewSettings(_view);
        var area = new TermRect(x, y, w, h);
        if (!_activated)
        {
            _view.Activate(r, area);
            _activated = true;
        }

        _view.Render(r, area);
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y)
    {
        PanelLine(r, w, y++, " VIC-20 NTSC", ConsoleColor.Cyan);
        y++;
        if (_machine == null)
        {
            PanelLine(r, w, y++, " (not loaded)");
            if (_loadError != null)
                PanelLine(r, w, y++, _loadError.Length > w - 2 ? _loadError[..(w - 2)] : _loadError);
            return;
        }

        var cpu = _machine.Board.Cpu;
        PanelLine(r, w, y++, $" PC ${cpu.Regs.PC:X4}");
        PanelLine(r, w, y++, $" A  ${cpu.Regs.A:X2}");
        PanelLine(r, w, y++, $" X  ${cpu.Regs.X:X2}");
        PanelLine(r, w, y++, $" Y  ${cpu.Regs.Y:X2}");
        PanelLine(r, w, y++, $" SP ${cpu.Regs.SP:X2}");
        y++;
        PanelLine(r, w, y++, $" Screen ${ _machine.ScreenMemoryBase:X4}");
        PanelLine(r, w, y++, $" Size  {_machine.TextColumns}x{_machine.TextRows} ({_machine.ScreenMemorySize} B)");
        PanelLine(r, w, y++, $" View  {_view?.DisplayMode}");
        if (_view?.DisplayMode == Vic20DisplayMode.Graphics)
            PanelLine(r, w, y++, $" Gfx   {_view.GraphicsMode}");
        PanelLine(r, w, y++, $" Step: {_view?.SteppedCount}");
        PanelLine(r, w, y++, $" Cyc:  {_view?.TotalCycles}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++;
        PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan);
        y++;
        PanelLine(r, w, y++, " Type to input");
        PanelLine(r, w, y++, " F10   text / graphics");
        PanelLine(r, w, y++, " F12   release keys");
        PanelLine(r, w, y++, " F6    exit module");
    }

    protected override void RenderSecondaryPanel(ITerminalRenderer r, int x, int h)
    {
        var palette = ThemePalette;
        ClearPanel(r, x, 0, SecondaryPanelWidth, h, palette.PanelFg, palette.PanelBg);

        int y = 1;
        SecondaryPanelLine(r, y++, " VIC keys", ConsoleColor.Cyan);
        SecondaryPanelLine(r, y++, " Host  Label");
        y++;

        foreach (VicHostKeyBinding binding in VicHostKeyMap.PanelRows)
        {
            if (y >= h - 1)
                break;
            SecondaryPanelLine(r, y++, VicHostKeyMap.FormatPanelLine(binding, SecondaryPanelWidth));
        }
    }

    private void ReleaseAllKeys() => _view?.ReleaseAllHeldKeys();

    private void LoadMachine()
    {
        try
        {
            _loadError = null;
            _machine?.Dispose();
            _machine = Vic20Machine.Load("vic20-ntsc.json");
            _view = new Vic20View(_machine);
            _activated = false;
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
            Errors?.Add("VIC-20", ex);
            _view = null;
            _machine = null;
            _activated = false;
        }
    }
}
