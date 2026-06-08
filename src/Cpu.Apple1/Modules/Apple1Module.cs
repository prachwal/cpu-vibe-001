using Cpu.Apple1.Adapters;
using Cpu.Apple1.Devices;
using Cpu.Apple1.Rendering.Views;
using Cpu.Board.Core;
using Cpu.Module;
using Cpu.Tui;
using Cpu.Tui.Components;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Apple1.Modules;

public sealed class Apple1Module : ModuleBase
{
    private const int MinWidthForKeyPanel = PanelWidth + PanelWidth + 50;

    private Apple1View? _view;
    private MachineBoard? _board;
    private bool _activated;

    private readonly (string Label, string File, int Cols, int Rows)[] _profileData = ProfileHelper.LoadProfiles("apple-");
    private int _profileIndex;
    private readonly ModalListDialog _profileDialog;

    public override string Name => "Apple 1";
    public override bool WantsMouse => false;

    public Apple1Module(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors)
    {
        _profileDialog = new ModalListDialog("Select Apple 1 profile",
            _profileData.Select(p => p.Label).ToArray());
    }

    private bool IsWozProfile => _profileIndex == 0;

    protected override int SecondaryPanelWidth => PanelWidth;

    protected override int SecondaryPanelMinWidth => MinWidthForKeyPanel;

    protected override void OnActivateCore()
    {
        if (_profileIndex < _profileData.Length)
            LoadProfile(_profileData[_profileIndex].File);
    }

    protected override void OnDeactivateCore()
    {
        _view = null; _activated = false;
        _board?.Dispose(); _board = null;
    }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        if (_profileDialog.IsOpen)
        {
            bool consumed = _profileDialog.OnKey(key);
            if (_profileDialog.Result.HasValue)
            {
                SwitchToProfile(_profileDialog.Result.Value);
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
            case ConsoleKey.Escape:
                return false;
            case ConsoleKey.F5:
                _profileDialog.Open(_profileIndex);
                return true;
        }

        if (_view != null)
        {
            if (Apple1HostKeyMap.TryMapConsoleKey(key, IsWozProfile, out byte machineCode))
            {
                _view.EnqueueMachineKey(machineCode);
                return true;
            }

            if (key.KeyChar >= 0x20 && key.KeyChar < 0x7F
                && Apple1HostKeyMap.TryMapHostChar(key.KeyChar, IsWozProfile, out machineCode))
            {
                _view.EnqueueMachineKey(machineCode);
                return true;
            }
        }
        return false;
    }

    private void SwitchToProfile(int index)
    {
        _profileIndex = index;
        _board?.Dispose();
        if (index < _profileData.Length)
            LoadProfile(_profileData[index].File);
    }

    public override bool OnTick()
    {
        if (!IsActive || _view == null || _profileDialog.IsOpen) return false;
        _view.StepCpu(5000);
        return true;
    }

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        if (_view == null) return;
        SyncViewSettings(_view);
        var area = new TermRect(x, y, w, h);
        if (!_activated)
        {
            _view.Activate(r, new TermRect(x, y, w, h));
            _activated = true;
        }
        _view.Render(r, area);

        if (_profileDialog.IsOpen)
            _profileDialog.Render(r, w, h);
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y)
    {
        string name = _profileIndex < _profileData.Length ? _profileData[_profileIndex].Label : "?";
        PanelLine(r, w, y++, $" {name}", ConsoleColor.Cyan);
        y++;
        if (_view == null) { PanelLine(r, w, y++, " (not loaded)"); return; }
        var cpu = _board?.Cpu;
        if (cpu != null)
        {
            PanelLine(r, w, y++, $" PC ${cpu.Regs.PC:X4}");
            PanelLine(r, w, y++, $" A  ${cpu.Regs.A:X2}");
            PanelLine(r, w, y++, $" X  ${cpu.Regs.X:X2}");
            PanelLine(r, w, y++, $" Y  ${cpu.Regs.Y:X2}");
            PanelLine(r, w, y++, $" SP ${cpu.Regs.SP:X2}");
            y++;
            PanelLine(r, w, y++, $" Step: {_view.SteppedCount}");
            PanelLine(r, w, y++, $" Cyc:  {_view.TotalCycles}");
        }
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++; PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan); y++;
        PanelLine(r, w, y++, " F5    select profile");
        PanelLine(r, w, y++, " F8    exit module");
    }

    protected override void RenderSecondaryPanel(ITerminalRenderer r, int x, int h)
    {
        var palette = ThemePalette;
        ClearPanel(r, x, 0, SecondaryPanelWidth, h, palette.PanelFg, palette.PanelBg);

        int y = 1;
        SecondaryPanelLine(r, y++, " Apple keys", ConsoleColor.Cyan);
        SecondaryPanelLine(r, y++, " Host  Label Code");
        y++;

        foreach (Apple1HostKeyBinding binding in Apple1HostKeyMap.PanelRows)
        {
            if (y >= h - 1)
                break;
            SecondaryPanelLine(r, y++, Apple1HostKeyMap.FormatPanelLine(binding, SecondaryPanelWidth));
        }
    }

    private void LoadProfile(string profileFile)
    {
        try
        {
            string p = Path.Combine(AppContext.BaseDirectory, "profiles", profileFile);
            if (!File.Exists(p)) { Errors?.Add($"Profile not found: {p}"); return; }

            var profile = MachineBoard.LoadProfile(p);
            _board = new MachineBoard(profile);
            var pia = new PiaDevice(profile.Pia!.BaseAddress);
            _board.AttachDevice(pia);

            var display = new Apple1DisplayAdapter(
                profile.Display?.Cols ?? 40, profile.Display?.Rows ?? 24);
            var keyboard = new Apple1KeyboardAdapter();

            string[] allNames = _profileData.Select(p => p.Label).ToArray();
            string curName = _profileIndex < allNames.Length ? allNames[_profileIndex] : "?";
            _view = new Apple1View(_board, pia, display, keyboard,
                curName, allNames, _profileIndex);

            _activated = false;
        }
        catch (Exception ex)
        {
            Errors?.Add("Apple1", ex);
            _view = null; _activated = false;
        }
    }
}
