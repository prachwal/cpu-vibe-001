namespace CpuVibe.Instructions.Php;

/// <summary>
/// PHP — Push Processor Status
/// Opcode: $08 | Size: 1 byte | Cycles: 3
/// Push P with B and bit 5 set
/// </summary>
public static class PhpImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.StackPush((byte)(cpu.Regs.P | CpuFlags.Break | CpuFlags.Unused));
        cpu.Cycles += 3;
    }
}
