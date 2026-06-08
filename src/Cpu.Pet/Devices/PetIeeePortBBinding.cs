namespace Cpu.Pet.Devices;

public sealed class PetIeeePortBBinding : IPortBinding
{
    private readonly PetKeyboardMatrix _matrix;
    private readonly PetIeeeBus _bus;

    public PetIeeePortBBinding(PetKeyboardMatrix matrix, PetIeeeBus bus)
    {
        _matrix = matrix;
        _bus = bus;
    }

    public bool HasInputReady => false;
    public bool IsOutputReady => false;

    public byte ReadPins()
    {
        byte columns = _matrix.ReadColumns();
        byte dio = _bus.GetCurrentDio();
        return (byte)(columns & dio);
    }

    public void WritePins(byte value, byte ddMask)
    {
        if (ddMask == 0xFF)
            _bus.OnDioWrite(value);
    }
}
