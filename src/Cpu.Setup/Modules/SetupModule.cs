using Cpu.Module;
using Cpu.Tui;
using Cpu.Tui.Configuration;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Input;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Setup.Modules;

public sealed class SetupModule : ModuleBase
{
    private readonly TuiSettingsStore _store;
    private TuiAppSettings _draft = TuiAppSettings.Default.Clone();
    private int _selected;
    private string? _status;
    private int _listTop;

    private static readonly string[] RowLabels =
        ["Theme", "Frame style", "Panel side", "Default graphics", "Mouse input", "Save & apply"];

    public override string Name => "Setup";

    public SetupModule(ITuiAppConfiguration config, TuiSettingsStore store, ErrorCollector? errors = null)
        : base(config, errors)
    {
        _store = store;
    }

    protected override void OnActivateCore()
    {
        _draft = Config.Current.Clone();
        _selected = 0;
        _status = null;
    }

    protected override bool OnKeyCore(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                _selected = Math.Max(0, _selected - 1);
                return true;
            case ConsoleKey.DownArrow:
                _selected = Math.Min(RowLabels.Length - 1, _selected + 1);
                return true;
            case ConsoleKey.LeftArrow:
                AdjustSelected(-1);
                return true;
            case ConsoleKey.RightArrow:
                AdjustSelected(1);
                return true;
            case ConsoleKey.S:
                SaveAndApply();
                return true;
            case ConsoleKey.Enter when _selected == RowLabels.Length - 1:
                SaveAndApply();
                return true;
        }
        return false;
    }

    public override bool OnMouse(MouseEvent e)
    {
        if (e.IsMotion || e.IsRelease || e.Button != 0)
            return false;

        if (!MenuListHitTest.TryHitRow(e.X, e.Y, 0, RowLabels.Length, _listTop, 1, out int row))
            return false;

        if (row == RowLabels.Length - 1)
            SaveAndApply();
        else
            _selected = row;
        return true;
    }

    private void SaveAndApply()
    {
        Config.Apply(_draft.Clone());
        _draft = Config.Current.Clone();
        _status = "Saved & applied";
    }

    private void AdjustSelected(int delta)
    {
        if (_selected == RowLabels.Length - 1) return;
        switch (_selected)
        {
            case 0:
                _draft.Theme = CycleEnum(_draft.Theme, delta);
                break;
            case 1:
                _draft.FrameStyle = CycleEnum(_draft.FrameStyle, delta);
                break;
            case 2:
                _draft.PanelSide = CycleEnum(_draft.PanelSide, delta);
                break;
            case 3:
                _draft.DefaultGraphicsMode = CycleEnum(_draft.DefaultGraphicsMode, delta);
                break;
            case 4:
                _draft.MouseEnabled = !_draft.MouseEnabled;
                break;
        }
    }

    private static T CycleEnum<T>(T current, int delta) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        int idx = Array.IndexOf(values, current);
        idx = (idx + delta + values.Length) % values.Length;
        return values[idx];
    }

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        var area = new TermRect(x, y, w, h);
        var session = new PresentationSession(r, area);
        session.Clear();

        int top = Math.Max(1, (h - RowLabels.Length - 4) / 2);
        session.Write(1, top, "Application settings", ConsoleColor.Cyan, ConsoleColor.Black);
        top += 2;
        _listTop = top;

        for (int i = 0; i < RowLabels.Length; i++)
        {
            bool sel = i == _selected;
            string value = i switch
            {
                0 => _draft.Theme.ToString(),
                1 => _draft.FrameStyle.ToString(),
                2 => _draft.PanelSide.ToString(),
                3 => _draft.DefaultGraphicsMode.ToString(),
                4 => _draft.MouseEnabled ? "On" : "Off",
                _ => "Enter / S"
            };
            string line = sel ? $"> {RowLabels[i]}: {value}" : $"  {RowLabels[i]}: {value}";
            session.Write(1, top + i, line,
                sel ? ConsoleColor.Black : ConsoleColor.Gray,
                sel ? ConsoleColor.White : ConsoleColor.Black);
        }

        if (_status != null)
            session.Write(1, top + RowLabels.Length + 1, _status, ConsoleColor.Green, ConsoleColor.Black);

        session.Write(1, top + RowLabels.Length + 3,
            "<- / -> change   S save", ConsoleColor.DarkGray, ConsoleColor.Black);
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y)
    {
        PanelLine(r, w, y++, " Config file");
        y++;
        string path = _store.FilePath;
        if (path.Length > w - 1) path = "..." + path[^Math.Min(w - 4, path.Length)..];
        PanelLine(r, w, y++, $" {path}");
        y++;
        PanelLine(r, w, y++, $" Theme: {Config.Current.Theme}");
        PanelLine(r, w, y++, $" Frame: {Config.Current.FrameStyle}");
        PanelLine(r, w, y++, $" Panel: {Config.Current.PanelSide}");
        PanelLine(r, w, y++, $" Mouse: {(Config.Current.MouseEnabled ? "On" : "Off")}");
    }

    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        y++; PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan); y++;
        PanelLine(r, w, y++, " Up/Dn select");
        PanelLine(r, w, y++, " <-/-> adjust");
        PanelLine(r, w, y++, " S save apply");
    }
}
