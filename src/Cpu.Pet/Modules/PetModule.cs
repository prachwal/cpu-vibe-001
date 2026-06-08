using Cpu.Board.Core;
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
        Array.ConvertAll(ProfileHelper.LoadProfiles("pet-"),
            p => (p.Label, p.File, p.Cols, p.Rows));

    private PetView? _view;
    private PetMachine? _machine;
    private bool _activated;
    private string? _loadError;
    private int _currentModelIndex;
    private string _currentProfile = "pet-2001-32-b2.json";
    private readonly ModalListDialog _profileDialog;
    private ModalListDialog _mountDialog;
    private string _currentLabel = "PET 2001-32 Basic 2";
    private string _mountedDisk = "";
    private string _mountedDiskPath = "";
    private string[] _diskFiles = [];
    private bool _showDebug;

    private bool IsDiskNative =>
        _currentProfile.Contains("b4", StringComparison.OrdinalIgnoreCase);

    public override string Name => "Commodore PET";
    public override bool WantsMouse => false;

    public PetModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors)
    {
        _profileDialog = new ModalListDialog("Select PET model",
            PetModels.Select(m => m.Label).ToArray());
        _mountDialog = new ModalListDialog("Mount D64 disk", []);
    }

    private static string[] FindDiskFiles()
    {
        string disksDir = Path.Combine(AppContext.BaseDirectory, "roms", "pet-test-disks");
        try
        {
            if (!Directory.Exists(disksDir))
                return [];
            return Directory.GetFiles(disksDir, "*.d64")
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f)
                .ToArray();
        }
        catch
        {
            return [];
        }
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

        if (_mountDialog.IsOpen)
        {
            bool consumed = _mountDialog.OnKey(key);
            if (_mountDialog.Result.HasValue)
            {
                int idx = _mountDialog.Result.Value;
                if (idx >= 0 && idx < _diskFiles.Length)
                {
                    string file = _diskFiles[idx];
                    string path = Path.Combine(AppContext.BaseDirectory, "roms", "pet-test-disks", file);
                    MountDisk(path);
                }
                _mountDialog.Result = null;
            }
            return consumed;
        }

        switch (key.Key)
        {
            case ConsoleKey.F1:
            case ConsoleKey.F2:
            case ConsoleKey.F4:
            case ConsoleKey.F7:
            case ConsoleKey.F9:
            case ConsoleKey.F11:
            case ConsoleKey.Escape:
                return false;
            case ConsoleKey.F5:
                _profileDialog.Open(_currentModelIndex);
                return true;
            case ConsoleKey.F8:
                _showDebug = !_showDebug;
                return true;
            case ConsoleKey.F12:
                _diskFiles = FindDiskFiles();
                _mountDialog = new ModalListDialog("Mount D64 disk", _diskFiles);
                _mountDialog.Open(0);
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

    protected override int SecondaryPanelWidth => _showDebug ? 80 : PanelWidth;

    protected override int SecondaryPanelMinWidth => _showDebug ? 80 + 50 : MinWidthForKeyPanel;

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
        if (_mountDialog.IsOpen)
            _mountDialog.Render(r, w, h);
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
        if (IsDiskNative)
        {
            if (_mountedDisk.Length > 0)
                PanelLine(r, w, y++, $" Disk: {_mountedDisk}", ConsoleColor.Green);
            else
                PanelLine(r, w, y++, " Disk: (none)", ConsoleColor.Yellow);
        }
        else
        {
            PanelLine(r, w, y++, " Disk: n/a (BASIC 2)", ConsoleColor.DarkGray);
        }
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
        PanelLine(r, w, y++, " F12   mount D64 disk");
    }

    protected override void RenderSecondaryPanel(ITerminalRenderer r, int x, int h)
    {
        var palette = ThemePalette;
        int w = SecondaryPanelWidth;
        ClearPanel(r, x, 0, w, h, palette.PanelFg, palette.PanelBg);

        ConsoleColor bFg = ConsoleColor.DarkGray;
        ConsoleColor bBg = palette.PanelBg;

        for (int cx = 0; cx < w; cx++)
        {
            r.SetCell(x + cx, 0, cx == 0 ? '\u250C' : cx == w - 1 ? '\u2510' : '\u2500', bFg, bBg);
            r.SetCell(x + cx, h - 1, cx == 0 ? '\u2514' : cx == w - 1 ? '\u2518' : '\u2500', bFg, bBg);
        }
        for (int cy = 1; cy < h - 1; cy++)
        {
            r.SetCell(x, cy, '\u2502', bFg, bBg);
            r.SetCell(x + w - 1, cy, '\u2502', bFg, bBg);
        }

        int margin = 1;
        int iw = w - 2 * margin - 1;

        void WriteLine(int row, string text, ConsoleColor? fg = null)
        {
            if (row < 1 || row >= h - 1) return;
            int px = x + margin;
            ConsoleColor cf = fg ?? palette.PanelFg;
            for (int c = 0; c < iw; c++)
                r.SetCell(px + c, row, c < text.Length ? text[c] : ' ', cf, bBg);
        }

        if (_showDebug && _machine != null)
        {
            int y = 1;
            if (y < h - 1) WriteLine(y++, " IEEE-488 Trace", ConsoleColor.Cyan);
            if (y < h - 1) WriteLine(y++, $" F8 toggle");
            y++;

            var logs = _machine.IeeeBus.GetTraceLog();
            int startRow = Math.Max(0, logs.Count - (h - y - 2));
            for (int i = startRow; i < logs.Count && y < h - 2; i++)
            {
                var log = logs[i];
                var color = log.Type switch
                {
                    "CMD" => ConsoleColor.Yellow,
                    "TX" => ConsoleColor.Green,
                    "RX" => ConsoleColor.Cyan,
                    "ERR" => ConsoleColor.Red,
                    _ => palette.PanelFg
                };
                WriteLine(y++, log.Text, color);
            }
            return;
        }

        int yy = 1;
        if (yy < h - 1) WriteLine(yy++, " PET keys", ConsoleColor.Cyan);
        if (yy < h - 1) WriteLine(yy++, " Host  PET  Code");
        yy++;

        foreach (PetHostKeyBinding binding in PetHostKeyMap.PanelRows)
        {
            if (yy >= h - 1) break;
            WriteLine(yy++, PetHostKeyMap.FormatPanelLine(binding, w));
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

    private void MountDisk(string fullPath)
    {
        try
        {
            _machine?.MountDisk(fullPath);
            _mountedDisk = Path.GetFileNameWithoutExtension(fullPath);
            _mountedDiskPath = fullPath;
        }
        catch (Exception ex)
        {
            _mountedDisk = $"error: {ex.Message}";
            _mountedDiskPath = "";
        }
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

            if (_mountedDiskPath.Length > 0)
                MountDisk(_mountedDiskPath);
            else if (IsDiskNative)
                TryAutoMountDisk();
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

    private void TryAutoMountDisk()
    {
        string[] files = FindDiskFiles();
        if (files.Length == 0)
            return;
        string first = files[0];
        MountDisk(Path.Combine(AppContext.BaseDirectory, "roms", "pet-test-disks", first));
    }
}
