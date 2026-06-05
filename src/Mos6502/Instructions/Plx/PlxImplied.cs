namespace Mos6502.Instructions.Plx;

/// <summary>
/// PLX — Pull X (Implied)
/// Opcode: $FA | Size: 1 byte | Cycles: 2
/// X = StackPop()
/// Flags: N, Z
/// </summary>
public static class PlxImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.X = cpu.StackPop();
        cpu.Regs.SetNZ(cpu.Regs.X);
        cpu.Cycles += 2;
    }
}
