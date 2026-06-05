namespace CpuVibe.Instructions.Tya;

/// <summary>
/// TYA — Transfer Y to Accumulator
/// Opcode: $98 | Size: 1 byte | Cycles: 2
/// A = Y
/// Flags: N, Z
/// </summary>
public static class TyaImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.A = cpu.Regs.Y;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.A & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
