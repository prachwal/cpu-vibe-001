namespace CpuVibe.Instructions.Inc;

/// <summary>
/// INC A — Increment Accumulator (65C02)
/// Opcode: $1A | Size: 1 byte | Cycles: 2
/// A = A + 1
/// Flags: N, Z
/// </summary>
public static class IncAccumulator
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.A++;
        cpu.Regs.SetNZ(cpu.Regs.A);
        cpu.Cycles += 2;
    }
}
