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
    public ColorRamDevice ColorRam => _colorRam;
    public Vic20KeyboardMatrix Keyboard => _keyboard;
    public Vic20Video Video => _video;

    public Vic20Machine(MachineProfile profile)
    {
        _board = new MachineBoard(profile);
        _vic = new Vic6560Device(Vic20MemoryMap.VicBaseAddress);
        _via = new Via6522Device(Vic20MemoryMap.ViaBaseAddress, Vic20MemoryMap.ColorRamStart);
        _colorRam = new ColorRamDevice();
        _keyboardBinding = new Vic20KeyboardViaBinding(_keyboard);

        _board.AttachDevice(_colorRam);
        _board.AttachDevice(_vic);
        _board.AttachDevice(_via);

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
        _colorRam.Reset();
        _prevCpuCycles = (ulong)_board.Cpu.Cycles;
        _accumulatedCycles = 0;
        SyncKeyboard();
    }

    public void Step(long cycles = 1)
    {
        for (long i = 0; i < cycles; i++)
        {
            SyncKeyboard();
            _via.Chip.Update();
            UpdateIrq();
            _board.Step();
            AdvanceRaster();
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

    public void RenderVideo() => _video.RenderFrame();

    public ushort GetCursorScreenAddress() =>
        (ushort)(_board.Bus.Read(0xD1) | (_board.Bus.Read(0xD2) << 8));

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

    private void UpdateIrq() => _board.Cpu.IrqAsserted = _via.HasInterrupt;

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
