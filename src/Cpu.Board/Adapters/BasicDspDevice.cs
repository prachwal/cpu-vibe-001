using CpuBase;

namespace Cpu.Board.Core.Adapters;

public sealed class BasicDspDevice : IDevice
{
    private readonly ushort _address;
    private readonly Action<byte> _onWrite;

    public string Name => "BasicDSP";

    public BasicDspDevice(ushort address, Action<byte> onWrite)
    {
        _address = address;
        _onWrite = onWrite;
    }

    public bool Accepts(ushort address) => address == _address;

    public byte Read(ushort address) => 0x00;

    public void Write(ushort address, byte value) => _onWrite(value);

    public void Reset() { }
}
