using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;
using Cpu.Vic20.System;

namespace Cpu.Vic20.Rendering.Views;

public sealed class Vic20View : BaseTermView, ITuiSettingsConsumer
{
    private const long CyclesPerFrame = 18_000;
    private const long CyclesPerKeyTap = 250_000;
    private const long CyclesAfterKeyRelease = 50_000;

    private readonly Vic20Machine _machine;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    private long _totalCycles;
    private int _steppedCount;
    private readonly HashSet<(int Row, int Col)> _heldKeys = [];

    public override string Name => "Commodore VIC-20";
    public Vic20Machine Machine => _machine;
    public long TotalCycles => _totalCycles;
    public int SteppedCount => _steppedCount;
    public TerminalGraphicsMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public Vic20View(Vic20Machine machine) => _machine = machine;

    public void ApplySettings(TuiAppSettings settings) => _frameStyle = settings.FrameStyle;

    public void CycleMode() => _mode = TerminalGraphicsModes.Next(_mode);

    protected override void Seed()
    {
        _machine.Reset();
        _totalCycles = 0;
        _steppedCount = 0;
        _heldKeys.Clear();
    }

    public override void Activate(ITerminalRenderer r, TermRect area)
    {
        if (!_seeded) { Seed(); _seeded = true; }
        TermArea.Clear(r, area, TerminalCell.Black, "Vic20View.Activate");
    }

    public override void Deactivate(ITerminalRenderer r, TermRect area)
    {
        ReleaseAllHeldKeys();
        TermArea.Clear(r, area, TerminalCell.Black, "Vic20View.Deactivate");
    }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        session.Clear();
        _machine.RenderVideo();
        PixelBuffer buffer = _machine.Video.ToPixelBuffer();
        (int cols, int rows) = PresentationSession.FitImage(buffer, area.W - 2, area.H - 2, _mode);
        var frame = session.CenterFrame(cols, rows);
        var inner = session.DrawFrame(frame, _frameStyle, "Commodore VIC-20");
        session.RenderCanvas(buffer, _mode, inner);
    }

    public void StepCpu(long cycles = CyclesPerFrame)
    {
        _machine.Run(cycles);
        _totalCycles += cycles;
        _steppedCount++;
    }

    public void TapKey(int row, int col)
    {
        PressKey(row, col);
        StepCpu(CyclesPerKeyTap);
        ReleaseKey(row, col);
        StepCpu(CyclesAfterKeyRelease);
    }

    public void PressKey(int row, int col)
    {
        if (_heldKeys.Add((row, col)))
            _machine.PressKey(row, col);
    }

    public void ReleaseKey(int row, int col)
    {
        if (_heldKeys.Remove((row, col)))
            _machine.ReleaseKey(row, col);
    }

    public void ReleaseAllHeldKeys()
    {
        foreach ((int row, int col) in _heldKeys)
            _machine.ReleaseKey(row, col);
        _heldKeys.Clear();
    }
}
