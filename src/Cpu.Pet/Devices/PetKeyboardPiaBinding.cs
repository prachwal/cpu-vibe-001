namespace Cpu.Pet.Devices;

public sealed class PetKeyboardPiaBinding
{
    private readonly PetKeyboardMatrix _matrix;

    public PetKeyboardPiaBinding(PetKeyboardMatrix matrix) => _matrix = matrix;

    public bool HasInputReady => _matrix.AnyKeyPressed;
    public bool IsOutputReady => true;

    public byte ReadPins() => _matrix.ReadColumns();

    public void WritePins(byte value, byte directionMask)
    {
        byte outputValue = (byte)(value & directionMask);
        _matrix.CurrentRow = PetKeyboardMatrix.DecodeRowSelect(outputValue);
    }
}
