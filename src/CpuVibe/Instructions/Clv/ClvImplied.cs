namespace CpuVibe.Instructions.Clv;

/// <summary>
/// CLV — Clear Overflow Flag
/// Opcode: $B8 | Size: 1 byte | Cycles: 2
/// V = 0
/// </summary>
public static class ClvImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Overflow, false);
        cpu.Cycles += 2;
    }
}
