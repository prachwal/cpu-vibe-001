namespace Mos6502.Instructions.Tay;

/// <summary>
/// TAY — Transfer Accumulator to Y
/// Opcode: $A8 | Size: 1 byte | Cycles: 2
/// Y = A
/// Flags: N, Z
/// </summary>
public static class TayImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.Y = cpu.Regs.A;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.Y == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.Y & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
