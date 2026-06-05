namespace Mos6502.Instructions.Phy;

/// <summary>
/// PHY — Push Y (Implied)
/// Opcode: $5A | Size: 1 byte | Cycles: 2
/// StackPush(Y)
/// Flags: none
/// </summary>
public static class PhyImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.StackPush(cpu.Regs.Y);
        cpu.Cycles += 2;
    }
}
