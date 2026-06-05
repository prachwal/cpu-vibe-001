namespace Z80.Core;

public class Registers
{
    public byte A { get; set; }
    public byte F { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }

    public byte A_ { get; set; }
    public byte F_ { get; set; }
    public byte B_ { get; set; }
    public byte C_ { get; set; }
    public byte D_ { get; set; }
    public byte E_ { get; set; }
    public byte H_ { get; set; }
    public byte L_ { get; set; }

    public ushort IX { get; set; }
    public ushort IY { get; set; }
    public ushort SP { get; set; }
    public ushort PC { get; set; }
    public byte I { get; set; }
    public byte R { get; set; }

    public ushort AF
    {
        get => (ushort)((A << 8) | F);
        set { A = (byte)(value >> 8); F = (byte)(value & 0xFF); }
    }

    public ushort BC
    {
        get => (ushort)((B << 8) | C);
        set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
    }

    public ushort DE
    {
        get => (ushort)((D << 8) | E);
        set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
    }

    public ushort HL
    {
        get => (ushort)((H << 8) | L);
        set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
    }

    public bool IsSign => (F & (byte)CpuFlags.Sign) != 0;
    public bool IsZero => (F & (byte)CpuFlags.Zero) != 0;
    public bool IsHalfCarry => (F & (byte)CpuFlags.HalfCarry) != 0;
    public bool IsParityOverflow => (F & (byte)CpuFlags.ParityOverflow) != 0;
    public bool IsSubtract => (F & (byte)CpuFlags.Subtract) != 0;
    public bool IsCarry => (F & (byte)CpuFlags.Carry) != 0;

    public void Reset()
    {
        A = 0; F = 0; B = 0; C = 0; D = 0; E = 0; H = 0; L = 0;
        IX = 0; IY = 0; SP = 0xF000; PC = 0x0000;
        I = 0; R = 0;
    }

    public void SetFlag(CpuFlags flag, bool set)
    {
        if (set) F |= (byte)flag;
        else F &= (byte)~flag;
    }

    public void SetSZ(byte value)
    {
        SetFlag(CpuFlags.Sign, (value & 0x80) != 0);
        SetFlag(CpuFlags.Zero, value == 0);
    }

    public void SetSZPV(byte value)
    {
        SetSZ(value);
        SetFlag(CpuFlags.ParityOverflow, CalculateParity(value));
    }

    public static bool CalculateParity(byte value)
    {
        int bits = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((value & (1 << i)) != 0) bits++;
        }
        return (bits & 1) == 0;
    }
}
