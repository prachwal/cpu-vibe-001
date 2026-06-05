namespace CpuVibe.Instructions.Inx;

/// <summary>
/// INX — Increment X
/// Opcode: $E8 | Size: 1 byte | Cycles: 2
/// X = X + 1
/// Flags: N, Z
/// </summary>
public static class InxImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.X++;
        cpu.Regs.SetNZ(cpu.Regs.X);
        cpu.Cycles += 2;
    }
}
