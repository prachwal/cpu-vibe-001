using Cpu.Pet.System;
using Cpu.Tui;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Pet.Rendering.Views;

public sealed class PetView : BaseTermView, ITuiSettingsConsumer
{
    private const long CyclesPerFrame = 18_000;

    private readonly PetMachine _machine;
    private readonly int _cols;
    private readonly int _rows;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    private long _totalCycles;
    private int _steppedCount;

    public override string Name => "Commodore PET";
    public PetMachine Machine => _machine;
    public long TotalCycles => _totalCycles;
    public int SteppedCount => _steppedCount;

    public void ApplySettings(TuiAppSettings settings) => _frameStyle = settings.FrameStyle;

    public PetView(PetMachine machine)
    {
        _machine = machine;
        _cols = machine.Columns;
        _rows = machine.Rows;
    }

    protected override void Seed()
    {
        _machine.Reset();
        _totalCycles = 0;
        _steppedCount = 0;
    }

    public override void Activate(ITerminalRenderer r, TermRect area)
    {
        if (!_seeded) { Seed(); _seeded = true; }
        TermArea.Clear(r, area, TerminalCell.Black, "PetView.Activate");
    }

    public override void Deactivate(ITerminalRenderer r, TermRect area)
    {
        TermArea.Clear(r, area, TerminalCell.Black, "PetView.Deactivate");
    }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        session.Clear();

        int maxCols = Math.Min(_cols, Math.Max(1, area.W - 2));
        int maxRows = Math.Min(_rows, Math.Max(1, area.H - 2));
        var frame = session.CenterFrame(maxCols, maxRows);
        var inner = session.DrawFrame(frame, _frameStyle, "Commodore PET");

        (int cursorCol, int cursorRow) = _machine.GetCursorPosition();

        for (int row = 0; row < _rows && row < inner.H; row++)
        {
            for (int col = 0; col < _cols && col < inner.W; col++)
            {
                char ch = _machine.GetDisplayCell(col, row);
                bool isCursor = row == cursorRow && col == cursorCol;
                if (isCursor)
                    ch = ch == ' ' ? '_' : ch;

                ConsoleColor fg = isCursor ? ConsoleColor.White : ConsoleColor.Green;
                ConsoleColor bg = isCursor ? ConsoleColor.DarkGreen : ConsoleColor.Black;
                r.SetCell(inner.X + col, inner.Y + row, ch, fg, bg);
            }
        }
    }

    public void StepCpu(long cycles = CyclesPerFrame)
    {
        _machine.ProcessPendingInput();
        _machine.Run(cycles);
        _totalCycles += cycles;
        _steppedCount++;
    }

    public void EnqueueKey(char ch) => _machine.EnqueueChar(ch);

    public void EnqueuePetCode(byte petCode) => _machine.EnqueuePetCode(petCode);
}
