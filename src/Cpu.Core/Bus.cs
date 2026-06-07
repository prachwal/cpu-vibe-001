namespace CpuBase;

/// <summary>
/// A device attached to the bus (memory-mapped I/O, ROM, RAM, peripherals).
/// </summary>
public interface IDevice
{
    string Name { get; }
    bool Accepts(ushort address);
    bool HandlesWrite => true;
    byte Read(ushort address);
    void Write(ushort address, byte value);
    void Reset();
}

/// <summary>
/// Address bus with device routing. CPU reads/writes go through the bus,
/// which dispatches to the correct device based on address range.
/// </summary>
public interface IBus : IMemory
{
    void Attach(IDevice device);
    void Detach(IDevice device);
    IReadOnlyList<IDevice> Devices { get; }
}

/// <summary>
/// Default bus implementation: routes Read/Write to attached devices.
/// Unmapped addresses return the last value on the data bus (floating bus).
/// </summary>
public class Bus : IBus
{
    private readonly List<IDevice> _devices = new();
    private byte _lastReadValue;

    public IReadOnlyList<IDevice> Devices => _devices;
    public byte LastReadValue => _lastReadValue;

    public void Attach(IDevice device) => _devices.Add(device);

    public void Detach(IDevice device) => _devices.Remove(device);

    public byte Read(ushort address)
    {
        for (int i = _devices.Count - 1; i >= 0; i--)
        {
            if (_devices[i].Accepts(address))
                return _lastReadValue = _devices[i].Read(address);
        }
        return _lastReadValue;
    }

    public void Write(ushort address, byte value)
    {
        for (int i = _devices.Count - 1; i >= 0; i--)
        {
            IDevice device = _devices[i];
            if (device.Accepts(address) && device.HandlesWrite)
            {
                device.Write(address, value);
                return;
            }
        }
    }

    public void Load(ushort address, byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            Write((ushort)(address + i), data[i]);
    }

    public void Reset()
    {
        foreach (var device in _devices)
            device.Reset();
    }
}
