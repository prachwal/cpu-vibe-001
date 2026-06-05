namespace Mos6502.Instructions.Tax;

/// <summary>
/// TAX — Transfer Accumulator to X
/// Opcode: $AA | Size: 1 byte | Cycles: 2
/// X = A
/// Flags: N, Z
/// </summary>
public static class TaxImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.X = cpu.Regs.A;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
