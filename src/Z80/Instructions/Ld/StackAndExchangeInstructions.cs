namespace Z80.Instructions.Ld;

/// <summary>
/// PUSH rr — Push register pair to stack
/// Opcode: $C5,$D5,$E5,$F5 | Size: 1 byte | Cycles: 11
/// Flags: none affected
/// </summary>
public static class PushRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int rr = (opcode >> 4) & 3;
        ushort value = GetRr(cpu, rr);
        cpu.StackPush(value);
        cpu.Cycles += 11;
    }

    public static ushort GetRr(Cpu cpu, int rr)
    {
        return rr switch
        {
            0 => cpu.Regs.BC,
            1 => cpu.Regs.DE,
            2 => cpu.Regs.HL,
            3 => cpu.Regs.AF,
            _ => throw new ArgumentException($"Invalid rr index: {rr}")
        };
    }
}

/// <summary>
/// POP rr — Pop register pair from stack
/// Opcode: $C1,$D1,$E1,$F1 | Size: 1 byte | Cycles: 10
/// Flags: none affected (except POP AF which restores all flags)
/// </summary>
public static class PopRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int rr = (opcode >> 4) & 3;
        ushort value = cpu.StackPop();
        SetRr(cpu, rr, value);
        cpu.Cycles += 10;
    }

    private static void SetRr(Cpu cpu, int rr, ushort value)
    {
        switch (rr)
        {
            case 0: cpu.Regs.BC = value; break;
            case 1: cpu.Regs.DE = value; break;
            case 2: cpu.Regs.HL = value; break;
            case 3: cpu.Regs.AF = value; break;
        }
    }
}

/// <summary>
/// EX DE, HL — Exchange DE and HL
/// Opcode: $EB | Size: 1 byte | Cycles: 4
/// Flags: none affected
/// </summary>
public static class ExDeHl
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        (cpu.Regs.D, cpu.Regs.H) = (cpu.Regs.H, cpu.Regs.D);
        (cpu.Regs.E, cpu.Regs.L) = (cpu.Regs.L, cpu.Regs.E);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// EX AF, AF' — Exchange AF and AF'
/// Opcode: $08 | Size: 1 byte | Cycles: 4
/// Flags: all affected (swapped with shadow)
/// </summary>
public static class ExAfAf
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        (cpu.Regs.A, cpu.Regs.A_) = (cpu.Regs.A_, cpu.Regs.A);
        (cpu.Regs.F, cpu.Regs.F_) = (cpu.Regs.F_, cpu.Regs.F);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// EXX — Exchange BC,DE,HL with BC',DE',HL'
/// Opcode: $D9 | Size: 1 byte | Cycles: 4
/// Flags: none affected
/// </summary>
public static class Exx
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        (cpu.Regs.B, cpu.Regs.B_) = (cpu.Regs.B_, cpu.Regs.B);
        (cpu.Regs.C, cpu.Regs.C_) = (cpu.Regs.C_, cpu.Regs.C);
        (cpu.Regs.D, cpu.Regs.D_) = (cpu.Regs.D_, cpu.Regs.D);
        (cpu.Regs.E, cpu.Regs.E_) = (cpu.Regs.E_, cpu.Regs.E);
        (cpu.Regs.H, cpu.Regs.H_) = (cpu.Regs.H_, cpu.Regs.H);
        (cpu.Regs.L, cpu.Regs.L_) = (cpu.Regs.L_, cpu.Regs.L);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// EX (SP), HL — Exchange top of stack with HL
/// Opcode: $E3 | Size: 1 byte | Cycles: 19
/// Flags: none affected
/// </summary>
public static class ExSpHl
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte lo = cpu.Memory.Read(cpu.Regs.SP);
        byte hi = cpu.Memory.Read((ushort)(cpu.Regs.SP + 1));
        cpu.Memory.Write(cpu.Regs.SP, cpu.Regs.L);
        cpu.Memory.Write((ushort)(cpu.Regs.SP + 1), cpu.Regs.H);
        cpu.Regs.L = lo;
        cpu.Regs.H = hi;
        cpu.Cycles += 19;
    }
}
