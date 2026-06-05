namespace CpuVibe.Instructions.Nop;

/// <summary>
/// NOP — No Operation
/// Opcode: $EA | Size: 1 byte | Cycles: 2
/// Nic nie robi.
/// </summary>
public static class Nop
{
    public static void Execute(Cpu cpu)
    {
        cpu.Cycles += 2;
    }
}
