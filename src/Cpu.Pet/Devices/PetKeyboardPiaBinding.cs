namespace Cpu.Pet.Devices;

public sealed class PetKeyboardPiaBinding : IPortBinding
{
    private readonly PetKeyboardMatrix _matrix;

    public PetKeyboardPiaBinding(PetKeyboardMatrix matrix) => _matrix = matrix;

    public Func<bool>? EoiProvider { get; set; }

    public bool HasInputReady => _matrix.AnyKeyPressed;
    public bool IsOutputReady => true;

    public byte ReadPins() => (byte)(_matrix.ReadColumns() | (EoiProvider?.Invoke() == true ? 0x40 : 0));

    public void WritePins(byte value, byte directionMask)
    {
        byte outputValue = (byte)(value & directionMask);
        _matrix.CurrentRow = PetKeyboardMatrix.DecodeRowSelect(outputValue);
    }
}
