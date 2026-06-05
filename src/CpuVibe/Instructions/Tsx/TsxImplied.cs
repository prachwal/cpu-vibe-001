namespace CpuVibe.Instructions.Tsx;

/// <summary>
/// TSX — Transfer Stack Pointer to X
/// Opcode: $BA | Size: 1 byte | Cycles: 2
/// X = SP
/// Flags: N, Z
/// </summary>
public static class TsxImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.X = cpu.Regs.SP;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
