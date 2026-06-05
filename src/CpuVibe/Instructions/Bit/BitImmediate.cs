namespace CpuVibe.Instructions.Bit;

/// <summary>
/// BIT #imm — Test Immediate (65C02)
/// Opcode: $89 | Size: 2 bytes | Cycles: 2
/// Z = (A & imm) == 0
/// Flags: Z only (N, V not affected)
/// </summary>
public static class BitImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte operand = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.SetFlag(CpuFlags.Zero, (cpu.Regs.A & operand) == 0);
        cpu.Cycles += 2;
    }
}

/// <summary>
/// BIT zp,X — Test Zero Page,X (65C02)
/// Opcode: $34 | Size: 2 bytes | Cycles: 4
/// Z = A & [zp+X], N = [zp+X] bit 7, V = [zp+X] bit 6
/// </summary>
public static class BitZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)(zp + cpu.Regs.X);
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, (cpu.Regs.A & value) == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Overflow, (value & 0x40) != 0);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// BIT abs,X — Test Absolute,X (65C02)
/// Opcode: $3C | Size: 3 bytes | Cycles: 4
/// Z = A & [abs+X], N = [abs+X] bit 7, V = [abs+X] bit 6
/// </summary>
public static class BitAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort effective = (ushort)(addr + cpu.Regs.X);
        byte value = cpu.Memory.Read(effective);
        cpu.Regs.SetFlag(CpuFlags.Zero, (cpu.Regs.A & value) == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Overflow, (value & 0x40) != 0);
        cpu.Cycles += 4;
    }
}
