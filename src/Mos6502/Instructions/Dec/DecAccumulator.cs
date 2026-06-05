namespace Mos6502.Instructions.Dec;

/// <summary>
/// DEC A — Decrement Accumulator (65C02)
/// Opcode: $3A | Size: 1 byte | Cycles: 2
/// A = A - 1
/// Flags: N, Z
/// </summary>
public static class DecAccumulator
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.A--;
        cpu.Regs.SetNZ(cpu.Regs.A);
        cpu.Cycles += 2;
    }
}
