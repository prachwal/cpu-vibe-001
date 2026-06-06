namespace CpuBase;

/// <summary>
/// Shared 64KB memory implementation for all CPU emulators.
/// </summary>
public class Memory : IMemory
{
    private readonly byte[] _data = new byte[65536];

    public byte Read(ushort address) => _data[address];

    public void Write(ushort address, byte value) => _data[address] = value;

    public void WriteWord(ushort address, ushort value)
    {
        _data[address] = (byte)(value & 0xFF);
        _data[address + 1] = (byte)(value >> 8);
    }

    public void Load(ushort address, byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            _data[address + i] = data[i];
    }

    public void Reset()
    {
        Array.Clear(_data);
    }
}
