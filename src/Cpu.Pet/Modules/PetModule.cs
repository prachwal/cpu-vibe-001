using Cpu.Pet.Devices;
using Cpu.Pet.Rendering.Views;
using Cpu.Pet.System;
using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Pet.Modules;

public sealed class PetModule : ModuleBase
{
    private const int KeyPanelWidth = 30;
    private const int MinWidthForKeyPanel = PanelWidth + KeyPanelWidth + 50;

    private PetView? _view;
    private PetMachine? _machine;
    private bool _activated;
    private string? _loadError;
    private int _keyPanelOriginX;

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

        if (PetHostKeyMap.TryMapConsoleKey(key, out byte petCode))
        {
            _view.EnqueuePetCode(petCode);
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

    public override void OnRender(ITerminalRenderer r, int w, int h)
    {
        int contentH = h - 1;
        bool showLeftPanel = w >= 80 + PanelWidth + 6;
        bool showKeyPanel = w >= MinWidthForKeyPanel;

        int leftPanelX = 0;
        int contentX = 0;
        int contentW = w;
        int keyPanelX = w - KeyPanelWidth;

        if (showLeftPanel)
        {
            bool panelLeft = Config.Current.PanelSide == PanelSide.Left;
            leftPanelX = panelLeft ? 0 : w - PanelWidth;
            contentX = panelLeft ? PanelWidth + 2 : 0;
            contentW = w - PanelWidth - 2;
        }

        if (showKeyPanel)
            contentW -= KeyPanelWidth + 2;

        RenderContent(r, contentX, 0, Math.Max(1, contentW), contentH);

        if (showLeftPanel)
        {
            SetPanelOriginX(leftPanelX);
            var palette = ThemePalette;
            ClearPanel(r, leftPanelX, 0, PanelWidth, contentH, palette.PanelFg, palette.PanelBg);
            int y = 1;
            RenderPanelHeader(r, PanelWidth, ref y, palette);
            RenderPanelInfo(r, PanelWidth, ref y);
            RenderPanelControls(r, PanelWidth, ref y);
        }

        if (showKeyPanel)
            RenderKeyPanel(r, keyPanelX, contentH);
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
        PanelLine(r, w, y++, " Arrows nav");
        PanelLine(r, w, y++, " F11   exit module");
    }

    private void RenderKeyPanel(ITerminalRenderer r, int x, int h)
    {
        _keyPanelOriginX = x;
        var palette = ThemePalette;
        ClearPanel(r, x, 0, KeyPanelWidth, h, palette.PanelFg, palette.PanelBg);

        int y = 1;
        KeyPanelLine(r, y++, " PET keys", ConsoleColor.Cyan);
        KeyPanelLine(r, y++, " Host  PET  Code");
        y++;

        foreach (PetHostKeyBinding binding in PetHostKeyMap.PanelRows)
        {
            if (y >= h - 1)
                break;
            KeyPanelLine(r, y++, PetHostKeyMap.FormatPanelLine(binding, KeyPanelWidth));
        }
    }

    private void KeyPanelLine(ITerminalRenderer r, int y, string text, ConsoleColor? fg = null)
    {
        var palette = ThemePalette;
        if (text.Length > KeyPanelWidth)
            text = text[..KeyPanelWidth];
        else if (text.Length < KeyPanelWidth)
            text += new string(' ', KeyPanelWidth - text.Length);

        TermArea.Write(r, new TermRect(_keyPanelOriginX, 0, KeyPanelWidth, 100), 0, y, text,
            fg ?? palette.PanelFg, palette.PanelBg);
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
