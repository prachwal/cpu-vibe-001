using Cpu.C16.System;
using Cpu.Chips.TED7360;
using Cpu.Tui;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.C16.Rendering.Views;

public sealed class C16View : BaseTermView, ITuiSettingsConsumer
{
    private const long CyclesPerFrame = 18_000;

    private readonly C16Machine _machine;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    private long _totalCycles;
    private int _steppedCount;

    public override string Name => "Commodore 16";
    public C16Machine Machine => _machine;
    public long TotalCycles => _totalCycles;
    public int SteppedCount => _steppedCount;

    public void ApplySettings(TuiAppSettings settings) => _frameStyle = settings.FrameStyle;

    public C16View(C16Machine machine) => _machine = machine;

    protected override void Seed()
    {
        _machine.Reset();
        _totalCycles = 0;
        _steppedCount = 0;
    }

    public override void Activate(ITerminalRenderer r, TermRect area)
    {
        if (!_seeded) { Seed(); _seeded = true; }
        TermArea.Clear(r, area, TerminalCell.Black, "C16View.Activate");
    }

    public override void Deactivate(ITerminalRenderer r, TermRect area)
    {
        TermArea.Clear(r, area, TerminalCell.Black, "C16View.Deactivate");
    }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        session.Clear();

        _machine.RenderVideo();
        var pixels = _machine.Video.Pixels;

        int pxW = 40 * 8;
        int pxH = 25 * 8;
        int termCols = pxW / 2;
        int termRows = pxH / 4;

        var frame = session.CenterFrame(termCols, termRows);
        var inner = session.DrawFrame(frame, _frameStyle, "Commodore 16");

        int originX = inner.X;
        int originY = inner.Y;

        for (int ty = 0; ty < termRows && ty < inner.H; ty++)
        {
            for (int tx = 0; tx < termCols && tx < inner.W; tx++)
            {
                int srcY = ty * 4;
                int srcX = tx * 2;
                int sumR = 0, sumG = 0, sumB = 0, count = 0;
                for (int dy = 0; dy < 4; dy++)
                {
                    for (int dx = 0; dx < 2; dx++)
                    {
                        int pi = (srcY + dy) * TED7360Constants.VideoWidth + (srcX + dx);
                        if (pi >= 0 && pi < pixels.Length)
                        {
                            var (rC, gC, bC) = TED7360Palette.ToRgb(pixels[pi]);
                            sumR += rC; sumG += gC; sumB += bC;
                            count++;
                        }
                    }
                }
                if (count > 0)
                {
                    byte avgR = (byte)(sumR / count);
                    byte avgG = (byte)(sumG / count);
                    byte avgB = (byte)(sumB / count);
                    int gray = (avgR + avgG + avgB) / 3;
                    char ch = gray > 0x80 ? '\u2591' : ' ';
                    var cFg = gray > 0x40 ? ConsoleColor.White : ConsoleColor.Gray;
                    var cBg = gray > 0x10 ? ConsoleColor.DarkGray : ConsoleColor.Black;
                    r.SetCell(originX + tx, originY + ty, ch, cFg, cBg);
                }
            }
        }
    }

    public void StepCpu(long cycles = CyclesPerFrame)
    {
        _machine.Run(cycles);
        _totalCycles += cycles;
        _steppedCount++;
    }
}
