using Cpu.Apple1.Adapters;
using Cpu.Board.Core;
using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Apple1.Rendering.Views;

public class Apple1View : BaseTermView, IPiaTerminal, ITuiSettingsConsumer
{
    private readonly MachineBoard _board;
    private readonly PiaDevice _pia;
    private readonly Apple1DisplayAdapter _display;
    private readonly Apple1KeyboardAdapter _keyboard;
    private readonly ushort _piaBase;
    private readonly int _cols;
    private readonly int _rows;
    private readonly string _profileName;
    private readonly string[] _roms;
    private readonly string[] _profiles;
    private int _profileIndex;
    private int _frameCount;
    private long _totalCycles;
    private readonly List<string> _bootLog = [];
    private ushort _lastPc;
    private int _steppedCount;
    private int _pendingDisplayChars;
    private FrameStyle _frameStyle = FrameStyle.Unicode;

    public override string Name => "Apple 1";
    public MachineBoard Board => _board;
    public Apple1DisplayAdapter Display => _display;
    public Apple1KeyboardAdapter Keyboard => _keyboard;
    public int FrameCount => _frameCount;
    public long TotalCycles => _totalCycles;
    public int SteppedCount => _steppedCount;
    public string ProfileName => _profileName;
    public int ProfileIndex => _profileIndex;
    public string[] Profiles => _profiles;

    public void ApplySettings(TuiAppSettings settings) => _frameStyle = settings.FrameStyle;

    public Apple1View(MachineBoard board, PiaDevice pia, Apple1DisplayAdapter display,
        Apple1KeyboardAdapter keyboard, string profileName = "Apple 1",
        string[]? profiles = null, int profileIndex = 0)
    {
        _board = board;
        _pia = pia;
        _display = display;
        _keyboard = keyboard;
        _piaBase = pia.BaseAddress;
        _cols = display.Cols;
        _rows = display.Rows;
        _profileName = profileName;
        _profiles = profiles ?? [profileName];
        _profileIndex = profileIndex;
        _pia.AttachTerminal(this);

        var basicDsp = new BasicDspDevice(0xD0F2, b => _display.Write(b));
        board.AttachDevice(basicDsp);

        var roms = new List<string>();
        foreach (var d in _board.Devices)
        {
            if (d is CpuBase.RomDevice rom)
                roms.Add($"${rom.Start:X4}-${rom.End:X4}");
            else if (d is CpuBase.RamDevice ram)
                roms.Add($"RAM ${ram.Start:X4}-${ram.End:X4}");
        }
        _roms = [..roms];
    }

    protected override void Seed()
    {
        _bootLog.Clear();
        AddLog($"{_profileName}");
        foreach (var r in _roms) AddLog(r);
        _board.Reset();

        _pia.Write(_piaBase, 0xFF);
        _pia.Write((ushort)(_piaBase + 2), 0x7F);
        _pia.Write((ushort)(_piaBase + 1), 0x04);
        _pia.Write((ushort)(_piaBase + 3), 0x04);
        _pia.Write(_piaBase, 0x00);
        _pia.Write((ushort)(_piaBase + 2), 0x00);

        _display.Clear();
        _lastPc = _board.Cpu.Regs.PC;
        _frameCount = 0;
        _totalCycles = 0;
        _steppedCount = 0;

        byte lo = _board.Cpu.Memory.Read(0xFFFC);
        byte hi = _board.Cpu.Memory.Read(0xFFFD);
        AddLog($"RST ${(hi << 8) | lo:X4}");
    }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        session.Clear();

        int maxCols = Math.Min(_cols, Math.Max(1, area.W - 2));
        int maxRows = Math.Min(_rows, Math.Max(1, area.H - 2));
        var frame = session.CenterFrame(maxCols, maxRows);
        var inner = session.DrawFrame(frame, _frameStyle, "Apple 1");

        for (int row = 0; row < _rows && row < inner.H; row++)
            for (int col = 0; col < _cols && col < inner.W; col++)
            {
                char ch = _display.GetChar(col, row);
                r.SetCell(inner.X + col, inner.Y + row, ch, ConsoleColor.Green, ConsoleColor.Black);
            }
    }

    private static void WriteLog(ITerminalRenderer r, TermRect area, int x, int y, string text, ConsoleColor? fg = null, ConsoleColor? bg = null)
    {
        if (y < 0 || y >= area.H || x >= area.W) return;
        int maxLen = area.W - x;
        if (text.Length > maxLen) text = text[..maxLen];
        if (text.Length > 0)
            TermArea.Write(r, area, x, y, text, fg ?? ConsoleColor.Gray, bg ?? ConsoleColor.Black);
    }

    public override void Activate(ITerminalRenderer r, TermRect area)
    {
        if (!_seeded) { Seed(); _seeded = true; }
        TermArea.Clear(r, area, TerminalCell.Black, "Apple1View.Activate");
    }

    public override void Deactivate(ITerminalRenderer r, TermRect area)
    {
        TermArea.Clear(r, area, TerminalCell.Black, "Apple1View.Deactivate");
    }

    public void StepCpu(long cycles = 2000)
    {
        _frameCount++;
        _pendingDisplayChars = 0;

        _pia.SetCb1(false);
        _pia.SetCb1(true);

        _board.Run(cycles);
        _totalCycles += cycles;
        _steppedCount++;

        _lastPc = _board.Cpu.Regs.PC;

        bool keyReady = (_pia.Read((ushort)(_piaBase + 1)) & 0x80) != 0;
        if (!keyReady && _keyboard.HasKey)
        {
            byte keyCode = _keyboard.ReadKey();
            _pia.Write(_piaBase, (byte)(keyCode | 0x80));
            _pia.SetCa1(true);
            _pia.SetCa1(false);
        }
    }

    private void AddLog(string msg)
    {
        _bootLog.Add(msg);
        if (_bootLog.Count > 12) _bootLog.RemoveAt(0);
    }

    public void OnPortAWrite(byte newValue, byte oldValue) { }

    public void OnPortBWrite(byte newValue, byte oldValue)
    {
        byte ch = (byte)(newValue & 0x7F);
        if (ch == 0x0D || ch == 0x0A)
            _display.Write(newValue);
        else if (ch >= 0x20)
            _display.Write(newValue);
        _pendingDisplayChars++;
    }

    public void OnIrq(IrqSource source) { }

    public void EnqueueKey(char ch)
    {
        _keyboard.EnqueueKey((byte)ch);
    }

    public void EnqueueMachineKey(byte machineCode)
    {
        _keyboard.EnqueueKey(machineCode);
    }

    public void EnqueueText(string text)
    {
        foreach (char ch in text)
            EnqueueKey(ch);
    }
}
