namespace Mos6502.Instructions.Pha;

/// <summary>
/// PHA — Push Accumulator
/// Opcode: $48 | Size: 1 byte | Cycles: 3
/// Push A
/// </summary>
public static class PhaImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.StackPush(cpu.Regs.A);
        cpu.Cycles += 3;
    }
}
