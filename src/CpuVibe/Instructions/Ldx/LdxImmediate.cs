namespace CpuVibe.Instructions.Ldx;

/// <summary>
/// LDX #imm — Load X Register (Immediate)
/// Opcode: $A2 | Size: 2 bytes | Cycles: 2
/// X = imm
/// Flags: N, Z
/// </summary>
public static class LdxImmediate
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.X = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
