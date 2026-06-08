using Cpu.Board.Core;
using Cpu.C16.Devices;
using Cpu.Chips.TED7360;
using Cpu.Tui.Graphics;

namespace Cpu.C16.System;

public sealed class C16Machine : IDisposable
{
    private readonly MachineBoard _board;
    private readonly TED7360Device _ted;
    private readonly C16MemoryDevice _memory;
    private readonly TED7360Video _video;
    private ulong _prevCpuCycles;
    private long _audioCycles;
    private float _lastAudioSample;

    public MachineBoard Board => _board;
    public TED7360Device Ted => _ted;
    public C16MemoryDevice Memory => _memory;
    public C16KeyboardMatrix Keyboard => _memory.Keyboard;
    public TED7360Video Video => _video;
    public TED7360Chip Chip => _ted.Chip;
    public float LastAudioSample => _lastAudioSample;

    public C16Machine(MachineProfile profile)
    {
        _board = new MachineBoard(profile);
        _ted = new TED7360Device(C16MemoryMap.TedBaseAddress);
        int ramSize = C16MemoryMapDefaults.DefaultRamSize;
        foreach (var m in profile.Memory)
            if (m.Type.Equals("ram", StringComparison.OrdinalIgnoreCase))
                ramSize = m.SizeBytes > 0 ? m.SizeBytes : ramSize;
        _memory = new C16MemoryDevice(
            LoadRom(profile, C16MemoryMap.BasicRomStart),
            LoadRom(profile, C16MemoryMap.KernalRomStart),
            _ted, ramSize);
        _board.AttachDevice(_memory);

        _video = new TED7360Video(_ted.Chip);
        _ted.Chip.KeyboardLookup = mask => _memory.Keyboard.ReadColumnsForRow(mask);

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
        _prevCpuCycles = (ulong)_board.Cpu.Cycles;
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

    public void PressKey(int row, int col)
    {
        _memory.Keyboard.Press(row, col);
        _memory.SyncKeyboard();
    }

    public void ReleaseKey(int row, int col)
    {
        _memory.Keyboard.Release(row, col);
        _memory.SyncKeyboard();
    }

    public void ReleaseAllKeys()
    {
        _memory.Keyboard.ReleaseAll();
        _memory.SyncKeyboard();
    }

    public void StepKeyboard(long keyCycles, long releaseCycles)
    {
        Run(keyCycles);
        ReleaseAllKeys();
        Run(releaseCycles);
    }

    public void FillKeyboardBuffer(byte petscii)
    {
        const ushort bufferCountAddress = 0x00EF;
        const ushort bufferStartAddress = 0x0527;
        const int maxKeyBuffer = 8;

        byte count = _board.Bus.Read(bufferCountAddress);
        if (count >= maxKeyBuffer)
            return;

        _board.Bus.Write((ushort)(bufferStartAddress + count), petscii);
        _board.Bus.Write(bufferCountAddress, (byte)(count + 1));
    }

    public void RenderVideo()
    {
        _video.RenderFrame(
            addr => _board.Bus.Read(addr),
            addr => ReadCharRom(addr));
    }

    public PixelBuffer ToPixelBuffer()
    {
        var buffer = new PixelBuffer(TED7360Constants.VideoWidth, TED7360Constants.VideoHeight);
        for (int y = 0; y < TED7360Constants.VideoHeight; y++)
        {
            for (int x = 0; x < TED7360Constants.VideoWidth; x++)
            {
                byte ci = _video.Pixels[y * TED7360Constants.VideoWidth + x];
                var (r, g, b) = TED7360Palette.ToRgb(ci);
                buffer.SetPixel(x, y, new Pixel(r, g, b));
            }
        }
        return buffer;
    }

    public void Dispose() => _board.Dispose();

    private void UpdateIrq() => _board.Cpu.IrqAsserted = _ted.HasInterrupt;

    private byte ReadCharRom(ushort address)
    {
        return _memory.Read((ushort)(C16MemoryMap.KernalRomStart + (address & 0x3FFF)));
    }

    private static byte[] LoadRom(MachineProfile profile, ushort start)
    {
        var region = profile.Memory.FirstOrDefault(r =>
            r.Type.Equals("rom", StringComparison.OrdinalIgnoreCase) && r.StartAddress == start);
        if (region?.File == null)
            return [];

        string romRoot = Path.Combine(AppContext.BaseDirectory, "roms");
        string profileDir = profile.Name.ToLowerInvariant().Replace(' ', '-');
        string romPath = Path.Combine(romRoot, profileDir, region.File);
        return File.Exists(romPath) ? File.ReadAllBytes(romPath) : [];
    }
}
