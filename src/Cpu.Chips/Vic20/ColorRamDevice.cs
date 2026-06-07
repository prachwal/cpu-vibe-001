using CpuBase;

namespace Cpu.Chips.Vic20;

public sealed class ColorRamDevice : IDevice
{
    private readonly byte[] _data;

    public ushort BaseAddress { get; }
    public string Name => "VIC20 Color RAM";

    public ColorRamDevice(ushort baseAddress = Vic20MemoryMap.ColorRamStart, int size = Vic20MemoryMap.ColorRamSize)
    {
        BaseAddress = baseAddress;
        _data = new byte[size];
    }

    public bool Accepts(ushort address) =>
        address >= BaseAddress && address < BaseAddress + _data.Length;

    public byte Read(ushort address) => (byte)(_data[address - BaseAddress] & 0x0F);

    public void Write(ushort address, byte value) => _data[address - BaseAddress] = (byte)(value & 0x0F);

    public void Reset() => Array.Clear(_data);

    public byte ReadNibble(int offset) => (byte)(_data[offset] & 0x0F);
}
