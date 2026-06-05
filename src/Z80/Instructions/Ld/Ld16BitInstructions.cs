namespace Z80.Instructions.Ld;

/// <summary>
/// LD rr, nn — Load register pair from immediate 16-bit
/// Opcode: $01,$11,$21,$31 | Size: 3 bytes | Cycles: 10
/// Flags: none affected
/// </summary>
public static class LdRrNn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        int rr = (opcode >> 4) & 3;
        SetRr(cpu, rr, nn);
        cpu.Cycles += 10;
    }

    public static void SetRr(Cpu cpu, int rr, ushort value)
    {
        switch (rr)
        {
            case 0: cpu.Regs.BC = value; break;
            case 1: cpu.Regs.DE = value; break;
            case 2: cpu.Regs.HL = value; break;
            case 3: cpu.Regs.SP = value; break;
        }
    }
}

/// <summary>
/// LD (nn), HL — Store HL to memory at nn
/// Opcode: $22 | Size: 3 bytes | Cycles: 16
/// Flags: none affected
/// </summary>
public static class LdNnHl
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        cpu.Memory.Write(nn, cpu.Regs.L);
        cpu.Memory.Write((ushort)(nn + 1), cpu.Regs.H);
        cpu.Cycles += 16;
    }
}

/// <summary>
/// LD HL, (nn) — Load HL from memory at nn
/// Opcode: $2A | Size: 3 bytes | Cycles: 16
/// Flags: none affected
/// </summary>
public static class LdHlNn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        cpu.Regs.L = cpu.Memory.Read(nn);
        cpu.Regs.H = cpu.Memory.Read((ushort)(nn + 1));
        cpu.Cycles += 16;
    }
}

/// <summary>
/// LD (nn), A — Store A to memory at nn
/// Opcode: $32 | Size: 3 bytes | Cycles: 13
/// Flags: none affected
/// </summary>
public static class LdNnA
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        cpu.Memory.Write(nn, cpu.Regs.A);
        cpu.Cycles += 13;
    }
}

/// <summary>
/// LD A, (nn) — Load A from memory at nn
/// Opcode: $3A | Size: 3 bytes | Cycles: 13
/// Flags: none affected
/// </summary>
public static class LdANn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        cpu.Regs.A = cpu.Memory.Read(nn);
        cpu.Cycles += 13;
    }
}

/// <summary>
/// LD SP, HL — Load SP from HL
/// Opcode: $F9 | Size: 1 byte | Cycles: 6
/// Flags: none affected
/// </summary>
public static class LdSpHl
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.SP = cpu.Regs.HL;
        cpu.Cycles += 6;
    }
}
