using Cpu.C16.Devices;
using Cpu.Chips.TED7360;
using CpuBase;

namespace Cpu.C16.System;

public sealed class C16MemoryDevice : IDevice
{
    private const int RamSize = 0x4000;
    private const int RomSize = 0x4000;

    private readonly byte[] _ram = new byte[RamSize];
    private readonly byte[] _basicRom;
    private readonly byte[] _kernalRom;
    private readonly TED7360Device _ted;
    private readonly C16KeyboardMatrix _keyboard = new();
    private bool _romSelected = true;
    private int _lowRomBank;
    private int _highRomBank;
    private byte _pio2KeyboardMask = 0xFF;

    public C16MemoryDevice(byte[] basicRom, byte[] kernalRom, TED7360Device ted)
    {
        _basicRom = NormalizeRom(basicRom);
        _kernalRom = NormalizeRom(kernalRom);
        _ted = ted;
    }

    public string Name => "C16 memory mapper";
    public bool RomSelected => _romSelected;
    public int LowRomBank => _lowRomBank;
    public int HighRomBank => _highRomBank;
    public C16KeyboardMatrix Keyboard => _keyboard;
    public byte Pio2KeyboardMask => _pio2KeyboardMask;

    public bool Accepts(ushort address) => true;

    public byte Read(ushort address)
    {
        if (IsTed(address))
            return _ted.Read(address);

        if (IsPio2(address))
            return _pio2KeyboardMask;

        if (!_romSelected)
            return ReadRam(address);

        if (address >= 0x8000 && address <= 0xBFFF)
            return _lowRomBank == 0 ? _basicRom[address & 0x3FFF] : (byte)0xFF;

        if (address >= 0xFC00 && address <= 0xFCFF)
            return _kernalRom[address & 0x3FFF];

        if (address >= 0xC000)
            return _highRomBank == 0 ? _kernalRom[address & 0x3FFF] : (byte)0xFF;

        return ReadRam(address);
    }

    public void Write(ushort address, byte value)
    {
        if (IsTed(address))
        {
            if (address == C16MemoryMap.SwitchRomAddress)
            {
                _romSelected = true;
                _ted.Chip.WriteByte(TED7360Constants.REG_FF13_CHARGEN, (byte)(_ted.Chip[TED7360Constants.REG_FF13_CHARGEN] | 0x01));
                return;
            }

            if (address == C16MemoryMap.SwitchRamAddress)
            {
                _romSelected = false;
                _ted.Chip.WriteByte(TED7360Constants.REG_FF13_CHARGEN, (byte)(_ted.Chip[TED7360Constants.REG_FF13_CHARGEN] & 0xFE));
                return;
            }

            _ted.Write(address, value);
            return;
        }

        if (IsPio2(address))
        {
            _pio2KeyboardMask = value;
            SyncKeyboard();
            return;
        }

        if (address >= 0xFDD0 && address <= 0xFDDF)
        {
            int bank = address & 0x0F;
            _lowRomBank = bank & 0x03;
            _highRomBank = (bank >> 2) & 0x03;
            return;
        }

        WriteRam(address, value);
    }

    public void Reset()
    {
        Array.Clear(_ram);
        _ted.Reset();
        _romSelected = true;
        _lowRomBank = 0;
        _highRomBank = 0;
        _pio2KeyboardMask = 0xFF;
        _keyboard.ReleaseAll();
        SyncKeyboard();
    }

    public byte ReadRam(ushort address) => _ram[address & 0x3FFF];

    public void SyncKeyboard()
    {
        _keyboard.SetRowMask(_pio2KeyboardMask);
        _ted.Chip.SetKeyboardColumns(_keyboard.ReadColumns());
    }

    private void WriteRam(ushort address, byte value) => _ram[address & 0x3FFF] = value;

    private static bool IsTed(ushort address) => address >= C16MemoryMap.TedBaseAddress && address <= C16MemoryMap.TedEndAddress;
    private static bool IsPio2(ushort address) => address >= C16MemoryMap.Pio2BaseAddress && address <= C16MemoryMap.Pio2EndAddress;

    private static byte[] NormalizeRom(byte[] data)
    {
        var rom = new byte[RomSize];
        Array.Fill<byte>(rom, 0xFF);
        Array.Copy(data, rom, Math.Min(data.Length, rom.Length));
        return rom;
    }
}
