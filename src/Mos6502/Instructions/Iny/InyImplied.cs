namespace Mos6502.Instructions.Iny;

/// <summary>
/// INY — Increment Y Register (Implied)
/// Opcode: $C8 | Size: 1 byte | Cycles: 2
/// Y = Y + 1
/// Flags: N, Z
/// </summary>
public static class InyImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.Y++;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.Y == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.Y & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
