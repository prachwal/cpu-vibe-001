public class Memory
{
    private readonly byte[] _ram = new byte[0x10000];

    public byte Read(ushort addr) => _ram[addr];

    public void Write(ushort addr, byte value) => _ram[addr] = value;

    public void WriteWord(ushort addr, ushort value)
    {
        _ram[addr] = (byte)(value & 0xFF);
        _ram[addr + 1] = (byte)(value >> 8);
    }

    public void Reset()
    {
        Array.Clear(_ram);
    }
}
