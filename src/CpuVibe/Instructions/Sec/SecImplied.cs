namespace CpuVibe.Instructions.Sec;

/// <summary>
/// SEC — Set Carry Flag
/// Opcode: $38 | Size: 1 byte | Cycles: 2
/// C = 1
/// </summary>
public static class SecImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Carry, true);
        cpu.Cycles += 2;
    }
}
