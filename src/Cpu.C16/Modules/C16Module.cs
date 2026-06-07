using Cpu.C16.Rendering.Views;
using Cpu.C16.System;
using Cpu.Chips.TED7360;
using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.C16.Modules;

public sealed class C16Module : ModuleBase
{
    private C16View? _view;
    private C16Machine? _machine;
    private bool _activated;
    private string? _loadError;

    public override string Name => "Commodore 16";
    public override bool WantsMouse => false;

    public C16Module(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }

    protected override void OnActivateCore()
    {
        LoadMachine();
        if (_view != null)
        {
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
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                return false;
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
        if (_view == null) return;

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
        PanelLine(r, w, y++, " Commodore C16", ConsoleColor.Cyan);
        y++;
        if (_machine == null)
        {
            PanelLine(r, w, y++, " (not loaded)");
            if (_loadError != null)
                PanelLine(r, w, y++, _loadError.Length > w - 2 ? _loadError[..(w - 2)] : _loadError);
            return;
        }
        PanelLine(r, w, y++, $" Step: {_view?.SteppedCount}");
        PanelLine(r, w, y++, $" Cyc:  {_view?.TotalCycles}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++;
        PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan);
        y++;
        PanelLine(r, w, y++, " Esc   exit module");
    }

    private void LoadMachine()
    {
        try
        {
            _loadError = null;
            _machine?.Dispose();
            _machine = C16Machine.Load("c16.json");
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
