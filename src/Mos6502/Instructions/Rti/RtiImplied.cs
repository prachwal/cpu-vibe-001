namespace Mos6502.Instructions.Rti;

/// <summary>
/// RTI — Return from Interrupt
/// Opcode: $40 | Size: 1 byte | Cycles: 6
/// Pull P, Pull PC
/// </summary>
public static class RtiImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.P = (CpuFlags)cpu.StackPop();
        byte lo = cpu.StackPop();
        byte hi = cpu.StackPop();
        cpu.Regs.PC = (ushort)((hi << 8) | lo);
        cpu.Cycles += 6;
    }
}
