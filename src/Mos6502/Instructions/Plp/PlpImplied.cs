namespace Mos6502.Instructions.Plp;

/// <summary>
/// PLP — Pull Processor Status
/// Opcode: $28 | Size: 1 byte | Cycles: 4
/// Pull P
/// </summary>
public static class PlpImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.P = (CpuFlags)(((byte)cpu.StackPop() & ~(byte)CpuFlags.Break) | (byte)CpuFlags.Unused);
        cpu.Cycles += 4;
    }
}
