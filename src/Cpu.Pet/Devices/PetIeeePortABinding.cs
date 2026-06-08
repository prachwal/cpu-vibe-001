namespace Cpu.Pet.Devices;

public sealed class PetIeeePortABinding : IPortBinding
{
    private readonly PetIeeeBus _bus;

    public PetIeeePortABinding(PetIeeeBus bus)
    {
        _bus = bus;
    }

    public bool HasInputReady => false;
    public bool IsOutputReady => false;

    public byte ReadPins() => (byte)(_bus.GetCurrentDio() ^ 0xFF);

    public void WritePins(byte value, byte ddMask) { }
}
