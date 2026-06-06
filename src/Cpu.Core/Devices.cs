namespace CpuBase;

/// <summary>
/// Simple RAM device.
/// </summary>
public class RamDevice : IDevice
{
    private readonly byte[] _data;
    private readonly ushort _start;
    private readonly ushort _end;

    public string Name { get; }
    public ushort Start => _start;
    public ushort End => _end;

    public RamDevice(string name, ushort start, ushort size)
    {
        Name = name;
        _start = start;
        _end = (ushort)(start + size - 1);
        _data = new byte[size];
    }

    public bool Accepts(ushort address) => address >= _start && address <= _end;

    public byte Read(ushort address) => _data[address - _start];

    public void Write(ushort address, byte value) => _data[address - _start] = value;

    public void Reset() => Array.Clear(_data);
}

/// <summary>
/// ROM device (read-only).
/// </summary>
public class RomDevice : IDevice
{
    private readonly byte[] _data;
    private readonly ushort _start;
    private readonly ushort _end;

    public string Name { get; }
    public ushort Start => _start;
    public ushort End => _end;

    public RomDevice(string name, ushort start, byte[] data)
    {
        Name = name;
        _start = start;
        _end = (ushort)(start + data.Length - 1);
        _data = (byte[])data.Clone();
    }

    public bool Accepts(ushort address) => address >= _start && address <= _end;

    public byte Read(ushort address) => _data[address - _start];

    public void Write(ushort address, byte value) { }

    public void Reset() { }
}
