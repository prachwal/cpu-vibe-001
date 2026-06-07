using Cpu.Board.Core;
using Cpu.Chips.Via6522;
using Cpu.Chips.Vic20;
using Cpu.Chips.Vic6560;
using Cpu.Vic20.Devices;
using Cpu.Vic20.Video;
using CpuBase;
using Mos6502.Core;

namespace Cpu.Vic20.System;

public sealed class Vic20Machine : IDisposable
{
    private readonly MachineBoard _board;
    private readonly Vic6560Device _vic;
    private readonly Via6522Device _via;
    private readonly Via6522Device _via2;
    private readonly ColorRamDevice _colorRam;
    private readonly RomDevice? _charRom;
    private readonly Vic20KeyboardMatrix _keyboard = new();
    private readonly Vic20KeyboardViaBinding _keyboardBinding;
    private readonly Vic20Video _video;
    private long _accumulatedCycles;
    private ulong _prevCpuCycles;

    public MachineBoard Board => _board;
    public Vic6560Device Vic => _vic;
    public Via6522Device Via => _via;
    public Via6522Device Via2 => _via2;
    public ColorRamDevice ColorRam => _colorRam;
    public Vic20KeyboardMatrix Keyboard => _keyboard;
    public Vic20Video Video => _video;

    public int TextColumns
    {
        get
        {
            int cols = _vic.Chip.Columns;
            return cols > 0 ? cols : Vic20Video.MaxCharsPerRow;
        }
    }

    public int TextRows
    {
        get
        {
            int rows = _vic.Chip.Rows;
            return rows > 0 ? rows : Vic20Video.MaxCharRows;
        }
    }

    public int ScreenMemoryBase => _vic.Chip.ScreenAddr;

    public int ScreenMemorySize => TextColumns * TextRows;

    public Vic20Machine(MachineProfile profile)
    {
        _board = new MachineBoard(profile);

        ushort vicBase = profile.Vic?.BaseAddress ?? Vic20MemoryMap.VicBaseAddress;
        ushort viaBase = profile.Via?.BaseAddress ?? Vic20MemoryMap.ViaBaseAddress;
        ushort viaMirrorEnd = profile.Via?.MirrorEndAddress ?? Vic20MemoryMap.ColorRamStart;

        _vic = new Vic6560Device(vicBase);
        _via = new Via6522Device(viaBase, viaMirrorEnd);
        _via2 = new Via6522Device(Vic20MemoryMap.Via2BaseAddress);
        _colorRam = new ColorRamDevice();
        _keyboardBinding = new Vic20KeyboardViaBinding(_keyboard);

        if (profile.Vic?.Pal == true)
            _vic.Chip.SetPalMode(true);

        _board.AttachDevice(_colorRam);
        _board.AttachDevice(_vic);
        _board.AttachDevice(_via);
        _board.AttachDevice(_via2);

        _charRom = FindCharRom();
        _video = new Vic20Video(
            _vic.Chip,
            address => _board.Bus.Read(address),
            ReadCharRom,
            address => _colorRam.Read(address));

        Reset();
    }

    public static Vic20Machine Load(string profileFile)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", profileFile);
        var profile = MachineBoard.LoadProfile(path);
        return new Vic20Machine(profile);
    }

    public void Reset()
    {
        _keyboard.ReleaseAll();
        _board.Reset();
        _vic.Reset();
        _via.Reset();
        _via2.Reset();
        _colorRam.Reset();
        _prevCpuCycles = (ulong)_board.Cpu.Cycles;
        _accumulatedCycles = 0;
        SyncKeyboard();
    }

    private long _audioCycles;
    private float _lastAudioSample;
    private static readonly double Phi2Freq = 1_431_818.0 / 14.0;

    public float LastAudioSample => _lastAudioSample;

    public void Step(long cpuCycles = 1)
    {
        long target = _board.Cpu.Cycles + cpuCycles;
        while (_board.Cpu.Cycles < target)
        {
            SyncKeyboard();
            long start = _board.Cpu.Cycles;
            _board.Step();
            long elapsed = _board.Cpu.Cycles - start;
            for (long i = 0; i < elapsed; i++)
            {
                _via.Chip.Update();
                _via2.Chip.Update();
            }
            UpdateIrq();
            AdvanceRaster();
            _audioCycles += elapsed;
        }
        if (_audioCycles >= 100)
        {
            double dt = _audioCycles / Phi2Freq;
            _vic.Chip.AdvanceOscillators(dt);
            _lastAudioSample = _vic.Chip.GetAudioSample();
            _audioCycles = 0;
        }
    }

    public void Run(long cycles) => Step(cycles);

    public void PressKey(int row, int col)
    {
        _keyboard.Press(row, col);
        SyncKeyboard();
    }

    public void ReleaseKey(int row, int col)
    {
        _keyboard.Release(row, col);
        SyncKeyboard();
    }

    public void ReleaseAllKeys()
    {
        _keyboard.ReleaseAll();
        SyncKeyboard();
    }

    public void StepKeyboard(long keyCycles, long releaseCycles)
    {
        Run(keyCycles);
        ReleaseAllKeys();
        Run(releaseCycles);
    }

    public void FillKeyboardBuffer(int row, int col)
    {
        if (!Devices.VicHostKeyMap.TryGetPetscii(row, col, out byte petscii))
            return;
        byte count = _board.Bus.Read(0xC6);
        if (count < 10)
        {
            _board.Bus.Write((ushort)(0x0277 + count), petscii);
            _board.Bus.Write(0xC6, (byte)(count + 1));
        }
    }

    public bool IsReverseCell(int col, int row)
    {
        int index = row * TextColumns + col;
        byte code = _board.Bus.Read((ushort)(ScreenMemoryBase + index));
        return (code & 0x80) != 0;
    }

    public void RenderVideo() => _video.RenderFrame();

    public ushort GetCursorScreenAddress() =>
        (ushort)(_board.Bus.Read(0xD1) | (_board.Bus.Read(0xD2) << 8));

    public char GetDisplayCell(int col, int row)
    {
        if ((uint)col >= (uint)TextColumns || (uint)row >= (uint)TextRows)
            return ' ';

        int index = row * TextColumns + col;
        byte code = _board.Bus.Read((ushort)(ScreenMemoryBase + index));
        return Vic20Scii.ToDisplayChar(code);
    }

    public (int Col, int Row) GetCursorPosition()
    {
        int cols = TextColumns;
        int rows = TextRows;
        ushort cursor = GetCursorScreenAddress();
        int baseAddr = ScreenMemoryBase;
        int offset = cursor - baseAddr;

        if (offset < 0 || offset >= cols * rows)
            return (0, 0);

        return (offset % cols, offset / cols);
    }

    public byte RawScreenByte(int col, int row)
    {
        if ((uint)col >= (uint)TextColumns || (uint)row >= (uint)TextRows)
            return 0;
        int index = row * TextColumns + col;
        return _board.Bus.Read((ushort)(ScreenMemoryBase + index));
    }

    public void Dispose() => _board.Dispose();

    private void SyncKeyboard() => _keyboardBinding.SyncToVia(_via.Chip);

    private void AdvanceRaster()
    {
        ulong delta = (ulong)_board.Cpu.Cycles - _prevCpuCycles;
        _prevCpuCycles = (ulong)_board.Cpu.Cycles;
        _accumulatedCycles += (long)delta;
        while (_accumulatedCycles >= _vic.Chip.CyclesPerLine)
        {
            _accumulatedCycles -= _vic.Chip.CyclesPerLine;
            _vic.Chip.Update();
        }
    }

    private void UpdateIrq() => _board.Cpu.IrqAsserted = _via.HasInterrupt || _via2.HasInterrupt;

    private RomDevice? FindCharRom()
    {
        foreach (IDevice device in _board.Devices)
        {
            if (device is RomDevice rom && rom.Start == Vic20MemoryMap.CharRomStart)
                return rom;
        }
        return null;
    }

    private byte ReadCharRom(ushort address)
    {
        if (_charRom == null)
            return 0xFF;
        if (_charRom.Accepts(address))
            return _charRom.Read(address);
        return _charRom.Read((ushort)(Vic20MemoryMap.CharRomStart + address));
    }
}
