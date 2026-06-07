using CpuBase;

namespace Cpu.Chips.Vic6560;

public sealed class Vic6560Device : IDevice
{
    private readonly Vic6560Chip _chip;

    public Vic6560Chip Chip => _chip;
    public ushort BaseAddress { get; }
    public string Name => "VIC6560";

    public Vic6560Device(ushort baseAddress = 0x9000)
    {
        BaseAddress = baseAddress;
        _chip = new Vic6560Chip(baseAddress);
    }

    public bool Accepts(ushort address) =>
        address >= BaseAddress && address < BaseAddress + Vic6560Constants.RegisterMirrorSize;

    public byte Read(ushort address) => _chip.ReadByte(address);

    public void Write(ushort address, byte value) => _chip.WriteByte(address, value);

    public void Reset() => _chip.Reset();

    public bool HasInterrupt => _chip.HasInterrupt;

    public bool AcknowledgeInterrupt() => _chip.AcknowledgeInterrupt();

    public void Tick(long cycles)
    {
        for (long i = 0; i < cycles; i++)
            _chip.Update();
    }
}
