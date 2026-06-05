namespace Z80.Core;

public class Memory
{
    private readonly byte[] _data = new byte[65536];

    public byte Read(ushort address) => _data[address];

    public void Write(ushort address, byte value) => _data[address] = value;

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
