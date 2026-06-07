using CpuBase;

namespace Cpu.Chips.TED7360;

public sealed class TED7360Device : IDevice
{
    private readonly TED7360Chip _chip;

    public TED7360Chip Chip => _chip;
    public ushort BaseAddress { get; }
    public string Name => "TED7360";

    public TED7360Device(ushort baseAddress = 0xFF00)
    {
        BaseAddress = baseAddress;
        _chip = new TED7360Chip();
    }

    public bool Accepts(ushort address) =>
        address >= BaseAddress && address <= BaseAddress + 0x3F;

    public byte Read(ushort address) =>
        _chip.ReadByte((ushort)(address & 0x3F));

    public void Write(ushort address, byte value) =>
        _chip.WriteByte((ushort)(address & 0x3F), value);

    public void Reset() => _chip.Reset();

    public bool HasInterrupt => _chip.HasInterrupt;

    public bool AcknowledgeInterrupt() => _chip.AcknowledgeInterrupt();
}
