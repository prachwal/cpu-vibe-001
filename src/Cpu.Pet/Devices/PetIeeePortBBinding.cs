namespace Cpu.Pet.Devices;

public sealed class PetIeeePortBBinding : IPortBinding
{
    private readonly PetIeeeBus _bus;
    private readonly bool _autoFlush;
    private readonly bool _invertOutput;
    private byte _latchedOutput;

    public PetIeeePortBBinding(PetIeeeBus bus, bool autoFlush = true, bool invertOutput = true)
    {
        _bus = bus;
        _autoFlush = autoFlush;
        _invertOutput = invertOutput;
    }

    public bool HasInputReady => false;
    public bool IsOutputReady => false;

    public byte ReadPins() => (byte)(_bus.GetCurrentDio() ^ 0xFF);

    public void WritePins(byte value, byte ddMask)
    {
        _latchedOutput = _invertOutput ? (byte)(value ^ 0xFF) : value;
        if (_autoFlush)
            FlushOutput();
    }

    public void FlushOutput() => _bus.OnDioWrite(_latchedOutput);
}
