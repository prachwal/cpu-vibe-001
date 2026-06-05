namespace CpuVibe.Instructions.Sed;

/// <summary>
/// SED — Set Decimal Mode
/// Opcode: $F8 | Size: 1 byte | Cycles: 2
/// D = 1
/// </summary>
public static class SedImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Decimal, true);
        cpu.Cycles += 2;
    }
}
