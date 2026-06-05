namespace CpuVibe.Instructions.Ldy;

/// <summary>
/// LDY #imm — Load Y Register (Immediate)
/// Opcode: $A0 | Size: 2 bytes | Cycles: 2
/// Y = imm
/// Flags: N, Z
/// </summary>
public static class LdyImmediate
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.Y = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.Y == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.Y & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
