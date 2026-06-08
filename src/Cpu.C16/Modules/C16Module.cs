using Cpu.C16.Devices;
using Cpu.C16.Rendering.Views;
using Cpu.C16.System;
using Cpu.Tui;
using Cpu.Tui.Components;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.C16.Modules;

public sealed class C16Module : ModuleBase
{
    private static readonly (string Label, string Profile)[] C16Models =
    [
        ("C16    NTSC  32KB  rev5",  "c16.json"),
        ("C116   PAL   16KB",        "c116.json"),
        ("Plus4  PAL   64KB  rev5",  "plus4.json"),
    ];

    private C16View? _view;
    private C16Machine? _machine;
    private bool _activated;
    private string? _loadError;
    private int _currentModelIndex;
    private string _currentProfile = "c16.json";
    private string _currentLabel = "C16 NTSC 32KB";
    private readonly ModalListDialog _profileDialog;

    public override string Name => "Commodore 16";
    public override bool WantsMouse => false;

    public C16Module(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors)
    {
        _profileDialog = new ModalListDialog("Select C16 model",
            C16Models.Select(m => m.Label).ToArray());
    }

    protected override void OnActivateCore()
    {
        LoadMachine();
        if (_view != null)
        {
            _view.DisplayMode = C16DisplayMode.Text;
            _view.GraphicsMode = Config.Current.DefaultGraphicsMode;
        }
    }

    protected override void OnDeactivateCore()
    {
        _view = null;
        _activated = false;
        _loadError = null;
        _machine?.Dispose();
        _machine = null;
    }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        if (_profileDialog.IsOpen)
        {
            bool consumed = _profileDialog.OnKey(key);
            if (_profileDialog.Result.HasValue)
            {
                int idx = _profileDialog.Result.Value;
                var model = C16Models[idx];
                _currentLabel = model.Label;
                _currentProfile = model.Profile;
                _currentModelIndex = idx;
                _profileDialog.Result = null;
                _view = null;
                _machine?.Dispose();
                _machine = null;
                _activated = false;
                _loadError = null;
                LoadMachine();
            }
            return consumed;
        }

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
            case ConsoleKey.F5:
                _profileDialog.Open(_currentModelIndex);
                return true;
        }

        if (_view == null)
            return false;

        if (key.Key == ConsoleKey.F10)
        {
            _view.ToggleDisplayMode();
            return true;
        }

        if (C16HostKeyMap.TryMapConsoleKey(key, out int row, out int col, out byte petscii))
        {
            _view.TapKey(row, col, petscii);
            return true;
        }

        return false;
    }

    public override bool OnTick()
    {
        if (!IsActive || _view == null || _profileDialog.IsOpen)
            return false;
        _view.StepCpu();
        return true;
    }

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        if (_view == null) return;

        SyncViewSettings(_view);
        var area = new TermRect(x, y, w, h);
        if (!_activated)
        {
            _view.Activate(r, area);
            _activated = true;
        }
        _view.Render(r, area);

        if (_profileDialog.IsOpen)
            _profileDialog.Render(r, w, h);
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y)
    {
        PanelLine(r, w, y++, $" {_currentLabel}", ConsoleColor.Cyan);
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
        PanelLine(r, w, y++, $" View  {_view?.DisplayMode}");
        if (_view?.DisplayMode == C16DisplayMode.Graphics)
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
        PanelLine(r, w, y++, " F5    select model");
        PanelLine(r, w, y++, " F10   text / graphics");
        PanelLine(r, w, y++, " Esc   exit module");
    }

    private void LoadMachine()
    {
        try
        {
            _loadError = null;
            _machine?.Dispose();
            _machine = C16Machine.Load(_currentProfile);
            _view = new C16View(_machine);
            _activated = false;
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
            Errors?.Add("C16", ex);
            _view = null;
            _machine = null;
            _activated = false;
        }
    }
}
