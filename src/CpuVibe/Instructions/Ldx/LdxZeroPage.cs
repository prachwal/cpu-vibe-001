namespace CpuVibe.Instructions.Ldx;

/// <summary>
/// LDX zp — Load X Register (Zero Page)
/// Opcode: $A6 | Size: 2 bytes | Cycles: 3
/// X = [zp]
/// Flags: N, Z
/// </summary>
public static class LdxZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.X = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 3;
    }
}
