namespace Z80.Instructions.Ld;

/// <summary>
/// LD r, r — Load register from register
/// Opcode: $40-$7F (except $76=HALT) | Size: 1 byte | Cycles: 4
/// Flags: none affected
/// </summary>
public static class LdRegisterToRegister
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int dst = (opcode >> 3) & 7;
        int src = opcode & 7;
        byte value = RegisterHelper.GetRegister(cpu, src);
        RegisterHelper.SetRegister(cpu, dst, value);
        cpu.Cycles += (src == 6 || dst == 6) ? (byte)7 : (byte)4;
    }
}

/// <summary>
/// LD r, n — Load register from immediate
/// Opcode: $06,$0E,$16,$1E,$26,$2E,$3E | Size: 2 bytes | Cycles: 7
/// Flags: none affected
/// </summary>
public static class LdRegisterFromImmediate
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int dst = (opcode >> 3) & 7;
        byte value = cpu.FetchByte();
        RegisterHelper.SetRegister(cpu, dst, value);
        cpu.Cycles += 7;
    }
}

/// <summary>
/// LD (HL), n — Load memory from immediate
/// Opcode: $36 | Size: 2 bytes | Cycles: 10
/// Flags: none affected
/// </summary>
public static class LdHlFromImmediate
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte value = cpu.FetchByte();
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Cycles += 10;
    }
}

/// <summary>
/// LD A, (BC) — Load A from memory at BC
/// Opcode: $02 | Size: 1 byte | Cycles: 7
/// Flags: none affected
/// </summary>
public static class LdAFromBc
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.A = cpu.Memory.Read(cpu.Regs.BC);
        cpu.Cycles += 7;
    }
}

/// <summary>
/// LD A, (DE) — Load A from memory at DE
/// Opcode: $12 | Size: 1 byte | Cycles: 7
/// Flags: none affected
/// </summary>
public static class LdAFromDe
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.A = cpu.Memory.Read(cpu.Regs.DE);
        cpu.Cycles += 7;
    }
}

/// <summary>
/// LD (BC), A — Store A to memory at BC
/// Opcode: $0A | Size: 1 byte | Cycles: 7
/// Flags: none affected
/// </summary>
public static class LdBcFromA
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Memory.Write(cpu.Regs.BC, cpu.Regs.A);
        cpu.Cycles += 7;
    }
}

/// <summary>
/// LD (DE), A — Store A to memory at DE
/// Opcode: $1A | Size: 1 byte | Cycles: 7
/// Flags: none affected
/// </summary>
public static class LdDeFromA
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Memory.Write(cpu.Regs.DE, cpu.Regs.A);
        cpu.Cycles += 7;
    }
}

/// <summary>
/// HALT — Halt processor
/// Opcode: $76 | Size: 1 byte | Cycles: 4
/// Flags: none affected
/// </summary>
public static class Halt
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Halted = true;
        cpu.Cycles += 4;
    }
}
