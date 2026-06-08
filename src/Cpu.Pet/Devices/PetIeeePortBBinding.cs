namespace Cpu.Pet.Devices;

public sealed class PetIeeePortBBinding : IPortBinding
{
    private readonly PetIeeeBus _bus;

    public PetIeeePortBBinding(PetIeeeBus bus)
    {
        _bus = bus;
    }

    public bool HasInputReady => false;
    public bool IsOutputReady => false;

    public byte ReadPins()
    {
        return _bus.GetCurrentDio();
    }

    public void WritePins(byte value, byte ddMask)
    {
        _bus.OnDioWrite(value);
    }
}
