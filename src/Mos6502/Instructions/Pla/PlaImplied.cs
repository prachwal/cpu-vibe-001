namespace Mos6502.Instructions.Pla;

/// <summary>
/// PLA — Pull Accumulator
/// Opcode: $68 | Size: 1 byte | Cycles: 4
/// Pull A
/// Flags: N, Z
/// </summary>
public static class PlaImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.A = cpu.StackPop();
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.A & 0x80) != 0);
        cpu.Cycles += 4;
    }
}
