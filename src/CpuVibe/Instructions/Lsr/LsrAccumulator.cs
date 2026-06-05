namespace CpuVibe.Instructions.Lsr;

/// <summary>
/// LSR A — Logical Shift Right (Accumulator)
/// Opcode: $4A | Size: 1 byte | Cycles: 2
/// C = A bit 0, A = A >> 1
/// Flags: N=0, Z, C
/// </summary>
public static class LsrAccumulator
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Carry, (cpu.Regs.A & 0x01) != 0);
        cpu.Regs.A = (byte)(cpu.Regs.A >> 1);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, false);
        cpu.Cycles += 2;
    }
}
