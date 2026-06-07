using CpuBase;

namespace Cpu.Chips.Via6522;

public sealed class Via6522Device : IDevice
{
    private readonly VIA6522Chip _chip;

    public VIA6522Chip Chip => _chip;
    public ushort BaseAddress { get; }
    public string Name => "VIA6522";

    public Via6522Device(ushort baseAddress)
    {
        BaseAddress = baseAddress;
        _chip = new VIA6522Chip(baseAddress);
    }

    public bool Accepts(ushort address) =>
        address >= BaseAddress && address < BaseAddress + VIA6522Constants.REGISTER_COUNT;

    public byte Read(ushort address) => _chip.ReadByte(address);

    public void Write(ushort address, byte value) => _chip.WriteByte(address, value);

    public void Reset() => _chip.Reset();

    public void Tick(long cycles)
    {
        for (long i = 0; i < cycles; i++)
            _chip.Update();
    }

    public bool HasInterrupt => _chip.HasInterrupt;

    public bool AcknowledgeInterrupt() => _chip.AcknowledgeInterrupt();
}
