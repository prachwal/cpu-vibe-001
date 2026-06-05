namespace CpuVibe.Instructions.Cld;

/// <summary>
/// CLD — Clear Decimal Mode
/// Opcode: $D8 | Size: 1 byte | Cycles: 2
/// D = 0
/// </summary>
public static class CldImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Decimal, false);
        cpu.Cycles += 2;
    }
}
