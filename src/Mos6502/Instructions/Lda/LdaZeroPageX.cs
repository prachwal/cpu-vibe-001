namespace Mos6502.Instructions.Lda;

/// <summary>
/// LDA zp,X — Load Accumulator from Zero Page,X
/// Opcode: $B5 | Size: 2 bytes | Cycles: 4
/// A = [(zp + X) & 0xFF]
/// </summary>
public static class LdaZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zpAddr = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zpAddr + cpu.Regs.X) & 0xFF);
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
        cpu.Cycles += 4;
    }
}
