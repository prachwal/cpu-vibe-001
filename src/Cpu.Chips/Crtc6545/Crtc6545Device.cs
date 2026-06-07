using CpuBase;

namespace Cpu.Chips.Crtc6545;

public sealed class Crtc6545Device : IDevice
{
    private readonly CRTC6545Chip _chip;

    public CRTC6545Chip Chip => _chip;
    public ushort BaseAddress { get; }
    public string Name => "CRTC6545";

    public Crtc6545Device(ushort baseAddress)
    {
        BaseAddress = baseAddress;
        _chip = new CRTC6545Chip(baseAddress);
    }

    public bool Accepts(ushort address) =>
        address >= BaseAddress && address < BaseAddress + 16;

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
