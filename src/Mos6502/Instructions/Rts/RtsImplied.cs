namespace Mos6502.Instructions.Rts;

/// <summary>
/// RTS — Return from Subroutine
/// Opcode: $60 | Size: 1 byte | Cycles: 6
/// Pull PC, PC = PC + 1
/// </summary>
public static class RtsImplied
{
    public static void Execute(Cpu cpu)
    {
        byte lo = cpu.StackPop();
        byte hi = cpu.StackPop();
        cpu.Regs.PC = (ushort)(((hi << 8) | lo) + 1);
        cpu.Cycles += 6;
    }
}
