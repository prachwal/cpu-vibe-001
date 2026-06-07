using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;
using Cpu.Vic20.System;

namespace Cpu.Vic20.Rendering.Views;

public enum Vic20DisplayMode
{
    Text,
    Graphics
}

public sealed class Vic20View : BaseTermView, ITuiSettingsConsumer
{
    public const int PetFrameColumns = 40;
    public const int PetFrameRows = 25;

    private const long CyclesPerFrame = 18_000;
    private const long CyclesPerKeyTap = 250_000;
    private const long CyclesAfterKeyRelease = 50_000;

    private readonly Vic20Machine _machine;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    private Vic20DisplayMode _displayMode = Vic20DisplayMode.Text;
    private TerminalGraphicsMode _graphicsMode = TerminalGraphicsMode.HalfBlockColor;
    private long _totalCycles;
    private int _steppedCount;
    private readonly HashSet<(int Row, int Col)> _heldKeys = [];

    public override string Name => "Commodore VIC-20";
    public Vic20Machine Machine => _machine;
    public long TotalCycles => _totalCycles;
    public int SteppedCount => _steppedCount;
    public Vic20DisplayMode DisplayMode
    {
        get => _displayMode;
        set => _displayMode = value;
    }

    public TerminalGraphicsMode GraphicsMode
    {
        get => _graphicsMode;
        set => _graphicsMode = value;
    }

    public Vic20View(Vic20Machine machine) => _machine = machine;

    public void ApplySettings(TuiAppSettings settings) => _frameStyle = settings.FrameStyle;

    public void ToggleDisplayMode()
    {
        if (_displayMode == Vic20DisplayMode.Text)
        {
            _displayMode = Vic20DisplayMode.Graphics;
            return;
        }

        _graphicsMode = TerminalGraphicsModes.Next(_graphicsMode);
        if (_graphicsMode == TerminalGraphicsMode.HalfBlockColor)
            _displayMode = Vic20DisplayMode.Text;
    }

    protected override void Seed()
    {
        _machine.Reset();
        _totalCycles = 0;
        _steppedCount = 0;
        _heldKeys.Clear();
        _displayMode = Vic20DisplayMode.Text;
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

        if (_displayMode == Vic20DisplayMode.Text)
            RenderTextScreen(r, session, area);
        else
            RenderGraphicsScreen(r, session, area);
    }

    private void RenderTextScreen(ITerminalRenderer r, PresentationSession session, TermRect area)
    {
        int frameCols = Math.Min(PetFrameColumns, Math.Max(1, area.W - 2));
        int frameRows = Math.Min(PetFrameRows, Math.Max(1, area.H - 2));
        var frame = session.CenterFrame(frameCols, frameRows);
        string title = $"VIC-20 screen RAM ${_machine.ScreenMemoryBase:X4}";
        var inner = session.DrawFrame(frame, _frameStyle, title);

        int cols = _machine.TextColumns;
        int rows = _machine.TextRows;
        int originX = inner.X + Math.Max(0, (inner.W - cols) / 2);
        int originY = inner.Y + Math.Max(0, (inner.H - rows) / 2);
        (int cursorCol, int cursorRow) = _machine.GetCursorPosition();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int x = originX + col;
                int y = originY + row;
                if (x >= inner.X + inner.W || y >= inner.Y + inner.H)
                    continue;

                char ch = _machine.GetDisplayCell(col, row);
                bool isCursor = row == cursorRow && col == cursorCol;
                bool reverse = IsReverseScreenCell(col, row);

                if (isCursor && ch == ' ')
                    ch = '_';

                ConsoleColor fg;
                ConsoleColor bg;
                if (isCursor)
                {
                    fg = ConsoleColor.White;
                    bg = ConsoleColor.DarkGreen;
                }
                else if (reverse)
                {
                    fg = ConsoleColor.Black;
                    bg = ConsoleColor.Green;
                }
                else
                {
                    fg = ConsoleColor.Green;
                    bg = ConsoleColor.Black;
                }

                r.SetCell(x, y, ch, fg, bg);
            }
        }
    }

    private bool IsReverseScreenCell(int col, int row)
    {
        int index = row * _machine.TextColumns + col;
        byte code = _machine.Board.Bus.Read((ushort)(_machine.ScreenMemoryBase + index));
        return (code & 0x80) != 0;
    }

    private void RenderGraphicsScreen(ITerminalRenderer r, PresentationSession session, TermRect area)
    {
        _machine.RenderVideo();
        PixelBuffer buffer = _machine.Video.ToPixelBuffer();
        (int cols, int rows) = PresentationSession.FitImage(buffer, area.W - 2, area.H - 2, _graphicsMode);
        var frame = session.CenterFrame(cols, rows);
        var inner = session.DrawFrame(frame, _frameStyle, "Commodore VIC-20");
        session.RenderCanvas(buffer, _graphicsMode, inner);
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
