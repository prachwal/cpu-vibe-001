using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;
using Cpu.Vic20.Devices;
using Cpu.Vic20.System;

namespace Cpu.Vic20.Rendering.Views;

public enum Vic20DisplayMode
{
    Text,
    Graphics
}

public enum KeyboardEchoMode
{
    Off,
    Overlay
}

public sealed record KeyEchoEntry(
    DateTime Time,
    string HostLabel,
    int Row,
    int Col,
    bool IsTap,
    string Action);

public sealed class Vic20View : BaseTermView, ITuiSettingsConsumer
{
    public const int PetFrameColumns = 40;
    public const int PetFrameRows = 25;

    private const long CyclesPerFrame = 18_000;
    private const long CyclesPerKeyTap = 250_000;
    private const long CyclesAfterKeyRelease = 50_000;
    private const int MaxEchoEntries = 6;
    private const int EchoOverlayHeight = 3;

    private readonly Vic20Machine _machine;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    private Vic20DisplayMode _displayMode = Vic20DisplayMode.Text;
    private TerminalGraphicsMode _graphicsMode = TerminalGraphicsMode.HalfBlockColor;
    private long _totalCycles;
    private int _steppedCount;
    private readonly HashSet<(int Row, int Col)> _heldKeys = [];
    private KeyboardEchoMode _echoMode = KeyboardEchoMode.Off;
    private readonly List<KeyEchoEntry> _keyEcho = [];

    public override string Name => "Commodore VIC-20";
    public Vic20Machine Machine => _machine;
    public long TotalCycles => _totalCycles;
    public int SteppedCount => _steppedCount;
    public KeyboardEchoMode EchoMode => _echoMode;
    public IReadOnlyList<KeyEchoEntry> KeyEcho => _keyEcho;
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

    public KeyboardEchoMode CycleEchoMode()
    {
        _echoMode = _echoMode switch
        {
            KeyboardEchoMode.Off => KeyboardEchoMode.Overlay,
            _ => KeyboardEchoMode.Off
        };
        if (_echoMode == KeyboardEchoMode.Off)
            _keyEcho.Clear();
        return _echoMode;
    }

    private void PushEchoEntry(string hostLabel, int row, int col, bool isTap, string action)
    {
        _keyEcho.Add(new KeyEchoEntry(DateTime.Now, hostLabel, row, col, isTap, action));
        while (_keyEcho.Count > MaxEchoEntries)
            _keyEcho.RemoveAt(0);
    }

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
        _keyEcho.Clear();
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

        var renderArea = area;
        int echoLines = 0;
        if (_echoMode == KeyboardEchoMode.Overlay)
        {
            echoLines = Math.Min(EchoOverlayHeight, Math.Max(3, area.H - 6));
            renderArea = new TermRect(area.X, area.Y, area.W, area.H - echoLines);
        }

        session.SetArea(renderArea);
        if (_displayMode == Vic20DisplayMode.Text)
            RenderTextScreen(r, session, renderArea);
        else
            RenderGraphicsScreen(r, session, renderArea);

        if (echoLines > 0)
        {
            session.SetArea(area);
            RenderEchoOverlay(r, session, area, echoLines);
        }
    }

    private void RenderEchoOverlay(ITerminalRenderer r, PresentationSession session, TermRect area, int lines)
    {
        int y0 = area.H - lines;
        int w = area.W;

        session.Write(0, y0, new string('\u2500', w), ConsoleColor.DarkGray, ConsoleColor.Black);

        if (_keyEcho.Count == 0)
        {
            string msg = " Keyboard Echo: press a key to see matrix mapping";
            if (msg.Length > w)
                msg = msg[..w];
            session.Write(0, y0 + 1, msg, ConsoleColor.DarkGray, ConsoleColor.Black);
            return;
        }

        for (int i = 0; i < lines - 1 && i < _keyEcho.Count; i++)
        {
            var entry = _keyEcho[^(i + 1)];
            string time = entry.Time.ToString("HH:mm:ss.fff")[^11..];
            string text = $" {time} [{entry.Action}] row={entry.Row} col={entry.Col} \"{entry.HostLabel}\"";
            if (text.Length > w)
                text = text[..w];
            session.Write(0, y0 + 1 + i, text, ConsoleColor.Gray, ConsoleColor.Black);
        }
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

    /// <summary>Places a PETSCII character in the KERNAL keyboard buffer ($0277).</summary>
    private void FillKeyboardBuffer(int row, int col)
    {
        if (!VicHostKeyMap.TryGetPetscii(row, col, out byte petscii))
            return;
        var bus = _machine.Board.Bus;
        byte count = bus.Read(0xC6);
        if (count < 10)
        {
            bus.Write((ushort)(0x0277 + count), petscii);
            bus.Write(0xC6, (byte)(count + 1));
        }
    }

    public void TapKey(int row, int col, string? label = null)
    {
        if (_echoMode != KeyboardEchoMode.Off)
            PushEchoEntry(label ?? $"({row},{col})", row, col, true, "tap");

        PressKey(row, col);
        StepCpu(CyclesPerKeyTap);
        ReleaseKey(row, col);
        StepCpu(CyclesAfterKeyRelease);

        FillKeyboardBuffer(row, col);
    }

    public void PressKey(int row, int col)
    {
        if (_heldKeys.Add((row, col)))
        {
            if (_echoMode != KeyboardEchoMode.Off)
                PushEchoEntry($"({row},{col})", row, col, false, "press");
            _machine.PressKey(row, col);
        }
    }

    public void ReleaseKey(int row, int col)
    {
        if (_heldKeys.Remove((row, col)))
        {
            if (_echoMode != KeyboardEchoMode.Off)
                PushEchoEntry($"({row},{col})", row, col, false, "release");
            _machine.ReleaseKey(row, col);
        }
    }

    public void ReleaseAllHeldKeys()
    {
        foreach ((int row, int col) in _heldKeys)
            _machine.ReleaseKey(row, col);
        _heldKeys.Clear();
    }
}
