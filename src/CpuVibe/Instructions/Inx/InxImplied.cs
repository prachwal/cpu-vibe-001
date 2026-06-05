namespace CpuVibe.Instructions.Inx;

/// <summary>
/// INX — Increment X Register (Implied)
/// Opcode: $E8 | Size: 1 byte | Cycles: 2
/// X = X + 1
/// Flags: N, Z
/// </summary>
public static class InxImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.X++;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
