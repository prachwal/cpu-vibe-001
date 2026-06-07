using Cpu.Chips.Via6522;

namespace Cpu.Vic20.Devices;

public sealed class Vic20KeyboardViaBinding
{
    private readonly Vic20KeyboardMatrix _matrix;

    public Vic20KeyboardViaBinding(Vic20KeyboardMatrix matrix) => _matrix = matrix;

    public void SyncToVia(VIA6522Chip via)
    {
        byte rowMask = (byte)(via.ORA & via.DDRA);
        _matrix.SetRowSelect(rowMask);
        via.PortBExternalInput = _matrix.ReadColumns();
    }
}
