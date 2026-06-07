using Cpu.Board.Core;
using Cpu.Chips.TED7360;
using CpuBase;

namespace Cpu.C16.System;

public sealed class C16Machine : IDisposable
{
    private readonly MachineBoard _board;
    private readonly TED7360Device _ted;
    private readonly TED7360Video _video;
    private long _accumulatedCycles;
    private ulong _prevCpuCycles;
    private long _audioCycles;
    private float _lastAudioSample;

    public MachineBoard Board => _board;
    public TED7360Device Ted => _ted;
    public TED7360Video Video => _video;
    public TED7360Chip Chip => _ted.Chip;
    public float LastAudioSample => _lastAudioSample;

    public C16Machine(MachineProfile profile)
    {
        _board = new MachineBoard(profile);
        _ted = new TED7360Device(C16MemoryMap.TedBaseAddress);
        _board.AttachDevice(_ted);

        _video = new TED7360Video(_ted.Chip);

        Reset();
    }

    public static C16Machine Load(string profileFile)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", profileFile);
        var profile = MachineBoard.LoadProfile(path);
        return new C16Machine(profile);
    }

    public void Reset()
    {
        _board.Reset();
        _ted.Reset();
        _prevCpuCycles = (ulong)_board.Cpu.Cycles;
        _accumulatedCycles = 0;
    }

    public void TickDevices(long cycles)
    {
        for (long i = 0; i < cycles; i++)
            _ted.Chip.Update();
    }

    public void Step(long cpuCycles = 1)
    {
        long target = _board.Cpu.Cycles + cpuCycles;
        while (_board.Cpu.Cycles < target)
        {
            long start = _board.Cpu.Cycles;
            TickDevices(1);
            UpdateIrq();
            _board.Step();
            _audioCycles += _board.Cpu.Cycles - start;
        }
        if (_audioCycles >= 100)
        {
            _lastAudioSample = 0;
            _audioCycles = 0;
        }
    }

    public void Run(long cycles) => Step(cycles);

    public void RenderVideo()
    {
        _video.RenderFrame(
            addr => _board.Bus.Read(addr),
            addr => ReadCharRom(addr));
    }

    public void Dispose() => _board.Dispose();

    private void UpdateIrq() => _board.Cpu.IrqAsserted = _ted.HasInterrupt;

    private byte ReadCharRom(ushort address)
    {
        if (address < 0xC000)
        {
            foreach (var dev in _board.Devices)
            {
                if (dev is RomDevice rom && rom.Start >= 0xC000 && rom.Accepts(address))
                    return rom.Read(address);
            }
            return _board.Bus.Read((ushort)(0xC000 + (address & 0x3FFF)));
        }
        return _board.Bus.Read(address);
    }
}
