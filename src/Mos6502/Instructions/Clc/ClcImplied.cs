namespace Mos6502.Instructions.Clc;

/// <summary>
/// CLC — Clear Carry Flag
/// Opcode: $18 | Size: 1 byte | Cycles: 2
/// C = 0
/// </summary>
public static class ClcImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Carry, false);
        cpu.Cycles += 2;
    }
}
