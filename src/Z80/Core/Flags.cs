namespace Z80.Core;

[Flags]
public enum CpuFlags : byte
{
    None = 0,
    Carry = 0x01,
    Subtract = 0x02,
    ParityOverflow = 0x04,
    HalfCarry = 0x10,
    Zero = 0x40,
    Sign = 0x80,
}
