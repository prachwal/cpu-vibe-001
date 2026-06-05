namespace Z80.Instructions.Alu;

/// <summary>
/// ADD HL, rr — Add register pair to HL
/// Opcode: $09,$19,$29,$39 | Size: 1 byte | Cycles: 11
/// Flags: H, N=0, C (PV unchanged)
/// </summary>
public static class AddHlRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int rr = (opcode >> 4) & 3;
        ushort value = GetRr(cpu, rr);
        int result = cpu.Regs.HL + value;

        cpu.Regs.SetFlag(CpuFlags.HalfCarry,
            ((cpu.Regs.HL & 0x0FFF) + (value & 0x0FFF)) > 0x0FFF);
        cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFFFF);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);

        cpu.Regs.HL = (ushort)result;
        cpu.Cycles += 11;
    }

    private static ushort GetRr(Cpu cpu, int rr)
    {
        return rr switch
        {
            0 => cpu.Regs.BC,
            1 => cpu.Regs.DE,
            2 => cpu.Regs.HL,
            3 => cpu.Regs.SP,
            _ => 0
        };
    }
}

/// <summary>
/// INC rr — Increment register pair
/// Opcode: $03,$13,$23,$33 | Size: 1 byte | Cycles: 6
/// Flags: none affected
/// </summary>
public static class IncRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int rr = (opcode >> 4) & 3;
        switch (rr)
        {
            case 0: cpu.Regs.BC++; break;
            case 1: cpu.Regs.DE++; break;
            case 2: cpu.Regs.HL++; break;
            case 3: cpu.Regs.SP++; break;
        }
        cpu.Cycles += 6;
    }
}

/// <summary>
/// DEC rr — Decrement register pair
/// Opcode: $0B,$1B,$2B,$3B | Size: 1 byte | Cycles: 6
/// Flags: none affected
/// </summary>
public static class DecRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int rr = (opcode >> 4) & 3;
        switch (rr)
        {
            case 0: cpu.Regs.BC--; break;
            case 1: cpu.Regs.DE--; break;
            case 2: cpu.Regs.HL--; break;
            case 3: cpu.Regs.SP--; break;
        }
        cpu.Cycles += 6;
    }
}

/// <summary>
/// RLCA — Rotate left circular accumulator
/// Opcode: $07 | Size: 1 byte | Cycles: 4
/// Flags: H=0, N=0, C=bit7
/// </summary>
public static class Rlca
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte bit7 = (byte)((a >> 7) & 1);
        cpu.Regs.A = (byte)((a << 1) | bit7);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7 != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// RRCA — Rotate right circular accumulator
/// Opcode: $0F | Size: 1 byte | Cycles: 4
/// Flags: H=0, N=0, C=bit0
/// </summary>
public static class Rrca
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte bit0 = (byte)(a & 1);
        cpu.Regs.A = (byte)((a >> 1) | (bit0 << 7));
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0 != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// RLA — Rotate left through carry accumulator
/// Opcode: $17 | Size: 1 byte | Cycles: 4
/// Flags: H=0, N=0, C=old bit7
/// </summary>
public static class Rla
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte oldCarry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;
        byte bit7 = (byte)((a >> 7) & 1);
        cpu.Regs.A = (byte)((a << 1) | oldCarry);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7 != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// RRA — Rotate right through carry accumulator
/// Opcode: $1F | Size: 1 byte | Cycles: 4
/// Flags: H=0, N=0, C=old bit0
/// </summary>
public static class Rra
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte oldCarry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;
        byte bit0 = (byte)(a & 1);
        cpu.Regs.A = (byte)((a >> 1) | (oldCarry << 7));
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0 != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// CPL — Complement accumulator
/// Opcode: $2F | Size: 1 byte | Cycles: 4
/// Flags: H=1, N=1
/// </summary>
public static class Cpl
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.A = (byte)~cpu.Regs.A;
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, true);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// SCF — Set carry flag
/// Opcode: $37 | Size: 1 byte | Cycles: 4
/// Flags: H=0, N=0, C=1
/// </summary>
public static class Scf
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.SetFlag(CpuFlags.Carry, true);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// CCF — Complement carry flag
/// Opcode: $3F | Size: 1 byte | Cycles: 4
/// Flags: H=old C, N=0, C=NOT C
/// </summary>
public static class Ccf
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        bool oldCarry = cpu.Regs.IsCarry;
        cpu.Regs.SetFlag(CpuFlags.Carry, !oldCarry);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, oldCarry);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// DAA — Decimal adjust accumulator
/// Opcode: $27 | Size: 1 byte | Cycles: 4
/// Flags: S, Z, H, PV(parity), N, C
/// </summary>
public static class Daa
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        bool nFlag = cpu.Regs.IsSubtract;
        bool hFlag = cpu.Regs.IsHalfCarry;
        bool cFlag = cpu.Regs.IsCarry;

        byte correction = 0;
        bool setCarry = false;

        if (a > 0x99 || cFlag)
        {
            correction = 0x60;
            setCarry = true;
        }

        if ((a & 0x0F) > 9 || hFlag)
            correction |= 0x06;

        if (nFlag)
            a -= correction;
        else
            a += correction;

        cpu.Regs.SetFlag(CpuFlags.Zero, a == 0);
        cpu.Regs.SetFlag(CpuFlags.Sign, (a & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, (!nFlag && (correction & 0x06) != 0) || (nFlag && hFlag));
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, Registers.CalculateParity(a));
        cpu.Regs.SetFlag(CpuFlags.Carry, setCarry);
        cpu.Regs.A = a;
        cpu.Cycles += 4;
    }
}
