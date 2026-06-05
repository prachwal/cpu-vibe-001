namespace Mos6502.Instructions.Sei;

/// <summary>
/// SEI — Set Interrupt Disable
/// Opcode: $78 | Size: 1 byte | Cycles: 2
/// I = 1
/// </summary>
public static class SeiImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Interrupt, true);
        cpu.Cycles += 2;
    }
}
