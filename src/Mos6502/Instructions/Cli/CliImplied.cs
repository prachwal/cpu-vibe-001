namespace Mos6502.Instructions.Cli;

/// <summary>
/// CLI — Clear Interrupt Disable
/// Opcode: $58 | Size: 1 byte | Cycles: 2
/// I = 0
/// </summary>
public static class CliImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Interrupt, false);
        cpu.Cycles += 2;
    }
}
