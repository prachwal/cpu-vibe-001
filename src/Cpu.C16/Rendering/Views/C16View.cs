using Cpu.C16.System;
using Cpu.Chips.TED7360;
using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.C16.Rendering.Views;

public enum C16DisplayMode
{
    Text,
    Graphics
}

public sealed class C16View : BaseTermView, ITuiSettingsConsumer
{
    public const int TextCols = 40;
    public const int TextRows = 25;
    private const long CyclesPerFrame = 18_000;

    private readonly C16Machine _machine;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    private C16DisplayMode _displayMode = C16DisplayMode.Text;
    private TerminalGraphicsMode _graphicsMode = TerminalGraphicsMode.HalfBlockColor;
    private long _totalCycles;
    private int _steppedCount;

    public override string Name => "Commodore 16";
    public C16Machine Machine => _machine;
    public long TotalCycles => _totalCycles;
    public int SteppedCount => _steppedCount;
    public C16DisplayMode DisplayMode
    {
        get => _displayMode;
        set => _displayMode = value;
    }

    public TerminalGraphicsMode GraphicsMode
    {
        get => _graphicsMode;
        set => _graphicsMode = value;
    }

    public void ApplySettings(TuiAppSettings settings) => _frameStyle = settings.FrameStyle;

    public C16View(C16Machine machine) => _machine = machine;

    public void ToggleDisplayMode()
    {
        if (_displayMode == C16DisplayMode.Text)
        {
            _displayMode = C16DisplayMode.Graphics;
            return;
        }
        _graphicsMode = TerminalGraphicsModes.Next(_graphicsMode);
        if (_graphicsMode == TerminalGraphicsMode.HalfBlockColor)
            _displayMode = C16DisplayMode.Text;
    }

    protected override void Seed()
    {
        _machine.Reset();
        _totalCycles = 0;
        _steppedCount = 0;
        _displayMode = C16DisplayMode.Text;
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

        if (_displayMode == C16DisplayMode.Text)
            RenderTextScreen(r, session, area);
        else
            RenderGraphicsScreen(r, session, area);
    }

    private void RenderTextScreen(ITerminalRenderer r, PresentationSession session, TermRect area)
    {
        int frameCols = Math.Min(TextCols, Math.Max(1, area.W - 2));
        int frameRows = Math.Min(TextRows, Math.Max(1, area.H - 2));
        var frame = session.CenterFrame(frameCols, frameRows);
        var inner = session.DrawFrame(frame, _frameStyle, "Commodore 16");

        int cols = TextCols;
        int rows = TextRows;
        int originX = inner.X + Math.Max(0, (inner.W - cols) / 2);
        int originY = inner.Y + Math.Max(0, (inner.H - rows) / 2);

        int screenAddr = ((_machine.Chip[0x14] & 0xF8) << 8) | 0x400;
        int colorAddr = (_machine.Chip[0x14] & 0xF8) << 8;
        var bus = _machine.Board.Bus;
        var chip = _machine.Chip;

        int cursorPos = ((chip[0x0C] & 3) << 8) | chip[0x0D];
        int cursorRow = cursorPos / cols;
        int cursorCol = cursorPos % cols;
        int flashPhase = (chip[0x1F] >> 3) & 0x0F;
        bool cursorVisible = flashPhase >= 8;

        if (cursorRow >= rows)
            cursorVisible = false;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int x = originX + col;
                int y = originY + row;
                if (x >= inner.X + inner.W || y >= inner.Y + inner.H)
                    continue;

                int idx = row * cols + col;
                byte screenCode = bus.Read((ushort)(screenAddr + idx));
                byte colorByte = bus.Read((ushort)(colorAddr + idx));

                bool isCursor = cursorVisible && row == cursorRow && col == cursorCol;

                char ch = CodeToDisplayChar(screenCode);
                bool reverse = (colorByte & 0x80) != 0;
                byte fgColor = (byte)(colorByte & 0x7F);

                ConsoleColor consoleFg = ConsoleColor.Green;
                ConsoleColor consoleBg = ConsoleColor.Black;

                if (isCursor)
                {
                    ch = '\u2588';
                    fgColor = 0;
                    consoleFg = ConsoleColor.White;
                    consoleBg = ConsoleColor.DarkGreen;
                    reverse = false;
                }
                else if (fgColor != 0)
                {
                    int chroma = fgColor & 0xF;
                    if (chroma != 0 && chroma <= 7)
                        consoleFg = (ConsoleColor)(chroma - 1);
                }

                if (reverse)
                    (consoleFg, consoleBg) = (consoleBg, consoleFg);

                r.SetCell(x, y, ch, consoleFg, consoleBg);
            }
        }
    }

    private void RenderGraphicsScreen(ITerminalRenderer r, PresentationSession session, TermRect area)
    {
        _machine.RenderVideo();
        var buffer = _machine.ToPixelBuffer();
        (int cols, int rows) = PresentationSession.FitImage(buffer, area.W - 2, area.H - 2, _graphicsMode);
        var frame = session.CenterFrame(cols, rows);
        var inner = session.DrawFrame(frame, _frameStyle, "Commodore 16");
        session.RenderCanvas(buffer, _graphicsMode, inner);
    }

    private static char CodeToDisplayChar(byte code)
    {
        code &= 0x7F;
        if (code >= 0x01 && code <= 0x1A) return (char)('A' + code - 1);
        if (code == 0x20) return ' ';
        if (code >= 0x21 && code <= 0x3F) return (char)code;
        if (code >= 0x40 && code <= 0x5A) return (char)(code + 0x20);
        return '.';
    }

    public void StepCpu(long cycles = CyclesPerFrame)
    {
        _machine.Run(cycles);
        _totalCycles += cycles;
        _steppedCount++;
    }
}
