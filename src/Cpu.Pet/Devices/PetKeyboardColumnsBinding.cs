namespace Cpu.Pet.Devices;

public sealed class PetKeyboardColumnsBinding : IPortBinding
{
    private readonly PetKeyboardMatrix _matrix;

    public PetKeyboardColumnsBinding(PetKeyboardMatrix matrix) => _matrix = matrix;

    public bool HasInputReady => _matrix.AnyKeyPressed;
    public bool IsOutputReady => true;

    public byte ReadPins() => _matrix.ReadColumns();

    public void WritePins(byte value, byte ddMask) { }
}
