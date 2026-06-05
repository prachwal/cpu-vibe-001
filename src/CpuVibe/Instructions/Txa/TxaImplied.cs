namespace CpuVibe.Instructions.Txa;

/// <summary>
/// TXA — Transfer X to Accumulator
/// Opcode: $8A | Size: 1 byte | Cycles: 2
/// A = X
/// Flags: N, Z
/// </summary>
public static class TxaImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.A = cpu.Regs.X;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.A & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
