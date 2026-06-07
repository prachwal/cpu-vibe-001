public class Registers
{
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte SP { get; set; }
    public ushort PC { get; set; }
    public CpuFlags P { get; set; }

    public bool IsZero => P.HasFlag(CpuFlags.Zero);
    public bool IsCarry => P.HasFlag(CpuFlags.Carry);
    public bool IsNegative => P.HasFlag(CpuFlags.Negative);
    public bool IsOverflow => P.HasFlag(CpuFlags.Overflow);
    public bool IsInterruptDisabled => P.HasFlag(CpuFlags.Interrupt);

    public void Reset()
    {
        A = 0; X = 0; Y = 0;
        SP = 0xFF;
        PC = 0x0000;
        P = CpuFlags.Unused | CpuFlags.Interrupt;
    }

    public void SetNZ(byte value)
    {
        SetFlag(CpuFlags.Zero, value == 0);
        SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
    }

    public void SetFlag(CpuFlags flag, bool set)
    {
        if (set) P |= flag;
        else P &= ~flag;
    }
}
