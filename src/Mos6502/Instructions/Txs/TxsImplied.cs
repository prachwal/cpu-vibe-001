namespace Mos6502.Instructions.Txs;

/// <summary>
/// TXS — Transfer X to Stack Pointer
/// Opcode: $9A | Size: 1 byte | Cycles: 2
/// SP = X
/// </summary>
public static class TxsImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SP = cpu.Regs.X;
        cpu.Cycles += 2;
    }
}
