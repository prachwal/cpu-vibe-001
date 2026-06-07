using Cpu.Pet.Devices;
using Cpu.Pet.Rendering.Views;
using Cpu.Pet.System;
using Cpu.Tui;
using Cpu.Tui.Components;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Pet.Modules;

public sealed class PetModule : ModuleBase
{
    private const int MinWidthForKeyPanel = PanelWidth + PanelWidth + 50;

    private static readonly (string Label, string Profile, int Cols, int Rows)[] PetModels =
    [
        ("PET 2001-8  8KB Basic 1  40×25", "pet-2001-8-b1.json", 40, 25),
        ("PET 2001-32 32KB Basic 2  40×25", "pet-2001-32-b2.json", 40, 25),
        ("PET 4032    32KB Basic 4  40×25", "pet-4032-b4.json", 40, 25),
        ("PET 8032    32KB Basic 4  80×25", "pet-8032-b4.json", 80, 25),
    ];

    private PetView? _view;
    private PetMachine? _machine;
    private bool _activated;
    private string? _loadError;
    private int _currentModelIndex;
    private string _currentProfile = "pet-2001-32-b2.json";
    private readonly ModalListDialog _profileDialog;
    private string _currentLabel = "PET 2001-32 Basic 2";

    public override string Name => "Commodore PET";
    public override bool WantsMouse => false;

    public PetModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors)
    {
        _profileDialog = new ModalListDialog("Select PET model",
            PetModels.Select(m => m.Label).ToArray());
    }

    protected override void OnActivateCore()
    {
        if (_machine == null)
            LoadMachine();
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
                var model = PetModels[idx];
                SwitchToModel(model.Label, model.Profile, model.Cols, model.Rows);
                _profileDialog.Result = null;
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
        if (!IsActive || _view == null || _profileDialog.IsOpen)
            return false;
        _view.StepCpu();
        return true;
    }

    protected override int SecondaryPanelWidth => PanelWidth;

    protected override int SecondaryPanelMinWidth => MinWidthForKeyPanel;

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
        PanelLine(r, w, y++, " F5    select model");
        PanelLine(r, w, y++, " F11   exit module");
    }

    protected override void RenderSecondaryPanel(ITerminalRenderer r, int x, int h)
    {
        var palette = ThemePalette;
        ClearPanel(r, x, 0, SecondaryPanelWidth, h, palette.PanelFg, palette.PanelBg);

        int y = 1;
        SecondaryPanelLine(r, y++, " PET keys", ConsoleColor.Cyan);
        SecondaryPanelLine(r, y++, " Host  PET  Code");
        y++;

        foreach (PetHostKeyBinding binding in PetHostKeyMap.PanelRows)
        {
            if (y >= h - 1)
                break;
            SecondaryPanelLine(r, y++, PetHostKeyMap.FormatPanelLine(binding, SecondaryPanelWidth));
        }
    }

    private void SwitchToModel(string label, string profile, int cols, int rows)
    {
        _currentLabel = label;
        _currentProfile = profile;
        _currentModelIndex = Array.FindIndex(PetModels, m => m.Profile == profile);
        if (_currentModelIndex < 0) _currentModelIndex = 0;
        _view = null;
        _machine?.Dispose();
        _machine = null;
        _activated = false;
        _loadError = null;
        LoadMachine(cols, rows);
    }

    private void LoadMachine(int cols = 40, int rows = 25)
    {
        try
        {
            _loadError = null;
            _machine?.Dispose();
            _machine = PetMachine.Load(_currentProfile, cols, rows);
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
