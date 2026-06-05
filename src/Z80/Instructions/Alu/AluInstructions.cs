namespace Z80.Instructions.Alu;

/// <summary>
/// ALU operation on register (ADD/ADC/SUB/SBC/AND/XOR/OR/CP A, r)
/// Opcode: $80-$BF (register variants)
/// Size: 1 byte | Cycles: 4 (register), 7 ((HL))
/// </summary>
public static class AluRegister
{
public static void Execute(Cpu cpu, byte opcode)
{
    int op = (opcode >> 3) & 7;
    int src = opcode & 7;
    byte value = RegisterHelper.GetRegister(cpu, src);
    byte a = cpu.Regs.A;

    if (op == 7)
    {
        AluHelper.CpA(cpu, a, value);
    }
    else
    {
        cpu.Regs.A = op switch
        {
            0 => AluHelper.AddA(cpu, a, value, false),
            1 => AluHelper.AddA(cpu, a, value, true),
            2 => AluHelper.SubA(cpu, a, value, false),
            3 => AluHelper.SubA(cpu, a, value, true),
            4 => AluHelper.AndA(cpu, a, value),
            5 => AluHelper.XorA(cpu, a, value),
            6 => AluHelper.OrA(cpu, a, value),
            _ => a
        };
    }

    cpu.Cycles += (src == 6) ? (byte)7 : (byte)4;
}
}

/// <summary>
/// ALU operation on immediate (ADD/ADC/SUB/SBC/AND/XOR/OR/CP A, n)
/// Opcode: $C6,$CE,$D6,$DE,$E6,$EE,$F6,$FE
/// Size: 2 bytes | Cycles: 7
/// </summary>
public static class AluImmediate
{
public static void Execute(Cpu cpu, byte opcode)
{
    int op = (opcode >> 3) & 7;
    byte n = cpu.FetchByte();
    byte a = cpu.Regs.A;

    if (op == 7)
    {
        AluHelper.CpA(cpu, a, n);
    }
    else
    {
        cpu.Regs.A = op switch
        {
            0 => AluHelper.AddA(cpu, a, n, false),
            1 => AluHelper.AddA(cpu, a, n, true),
            2 => AluHelper.SubA(cpu, a, n, false),
            3 => AluHelper.SubA(cpu, a, n, true),
            4 => AluHelper.AndA(cpu, a, n),
            5 => AluHelper.XorA(cpu, a, n),
            6 => AluHelper.OrA(cpu, a, n),
            _ => a
        };
    }

    cpu.Cycles += 7;
}
}

/// <summary>
/// INC r — Increment register
/// Opcode: $04,$0C,$14,$1C,$24,$2C,$3C
/// Size: 1 byte | Cycles: 4 (register), 11 ((HL))
/// Flags: S,Z,H,PV,N=0 (C unchanged)
/// </summary>
public static class IncRegister
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int reg = (opcode >> 3) & 7;
        byte value = RegisterHelper.GetRegister(cpu, reg);
        byte result = AluHelper.Inc(cpu, value);
        RegisterHelper.SetRegister(cpu, reg, result);
        cpu.Cycles += (reg == 6) ? (byte)11 : (byte)4;
    }
}

/// <summary>
/// DEC r — Decrement register
/// Opcode: $05,$0D,$15,$1D,$25,$2D,$3D
/// Size: 1 byte | Cycles: 4 (register), 11 ((HL))
/// Flags: S,Z,H,PV,N=1 (C unchanged)
/// </summary>
public static class DecRegister
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int reg = (opcode >> 3) & 7;
        byte value = RegisterHelper.GetRegister(cpu, reg);
        byte result = AluHelper.Dec(cpu, value);
        RegisterHelper.SetRegister(cpu, reg, result);
        cpu.Cycles += (reg == 6) ? (byte)11 : (byte)4;
    }
}
