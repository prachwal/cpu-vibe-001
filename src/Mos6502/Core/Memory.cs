using CpuBase;

namespace Mos6502.Core;

public class Memory : IMemory
{
    private readonly byte[] _ram = new byte[0x10000];

    public virtual byte Read(ushort addr) => _ram[addr];

    public virtual void Write(ushort addr, byte value) => _ram[addr] = value;

    public void WriteWord(ushort addr, ushort value)
    {
        _ram[addr] = (byte)(value & 0xFF);
        _ram[addr + 1] = (byte)(value >> 8);
    }

    public void Load(ushort address, byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            _ram[address + i] = data[i];
    }

    public void Reset()
    {
        Array.Clear(_ram);
    }
}
