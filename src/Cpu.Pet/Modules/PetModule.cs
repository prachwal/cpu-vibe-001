using Cpu.Pet.Rendering.Views;
using Cpu.Pet.System;
using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Pet.Modules;

public sealed class PetModule : ModuleBase
{
    private PetView? _view;
    private PetMachine? _machine;
    private bool _activated;
    private string? _loadError;

    public override string Name => "Commodore PET";
    public override bool WantsMouse => false;

    public PetModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }

    protected override void OnActivateCore() => LoadMachine();

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

        if (key.Key == ConsoleKey.Enter)
        {
            _view.EnqueueKey('\r');
            _view.StepCpu();
            return true;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            _view.EnqueueKey('\b');
            _view.StepCpu();
            return true;
        }

        if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F)
        {
            _view.EnqueueKey(key.KeyChar);
            _view.StepCpu();
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
        PanelLine(r, w, y++, " PET 2001-32", ConsoleColor.Cyan);
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
        PanelLine(r, w, y++, $" Step: {_view?.SteppedCount}");
        PanelLine(r, w, y++, $" Cyc:  {_view?.TotalCycles}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++;
        PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan);
        y++;
        PanelLine(r, w, y++, " Type to input");
        PanelLine(r, w, y++, " Enter send");
        PanelLine(r, w, y++, " F11   exit module");
    }

    private void LoadMachine()
    {
        try
        {
            _loadError = null;
            _machine?.Dispose();
            _machine = PetMachine.Load("pet-2001-32.json");
            _view = new PetView(_machine);
            _activated = false;
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
            Errors?.Add("PET", ex);
            RenderLog.Event("PetModule.Load", ex.Message);
            _view = null;
            _machine = null;
            _activated = false;
        }
    }
}
