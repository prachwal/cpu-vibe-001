using CpuBase;

namespace Cpu.Chips.Via6522;

public sealed class Via6522Device : IDevice
{
    private readonly VIA6522Chip _chip;

    public VIA6522Chip Chip => _chip;
    public ushort BaseAddress { get; }
    public string Name => "VIA6522";

    public Via6522Device(ushort baseAddress, ushort mirrorEndExclusive = 0)
    {
        BaseAddress = baseAddress;
        MirrorEndExclusive = mirrorEndExclusive != 0
            ? mirrorEndExclusive
            : (ushort)(baseAddress + VIA6522Constants.REGISTER_COUNT);
        _chip = new VIA6522Chip(baseAddress);
    }

    public ushort MirrorEndExclusive { get; }

    public bool Accepts(ushort address) =>
        address >= BaseAddress && address < MirrorEndExclusive;

    public byte Read(ushort address) => _chip.ReadByte(NormalizeAddress(address));

    public void Write(ushort address, byte value) => _chip.WriteByte(NormalizeAddress(address), value);

    public void Reset() => _chip.Reset();

    public void Tick(long cycles)
    {
        for (long i = 0; i < cycles; i++)
            _chip.Update();
    }

    public bool HasInterrupt => _chip.HasInterrupt;

    public bool AcknowledgeInterrupt() => _chip.AcknowledgeInterrupt();

    private ushort NormalizeAddress(ushort address) =>
        (ushort)(BaseAddress + (address & 0x0F));
}
