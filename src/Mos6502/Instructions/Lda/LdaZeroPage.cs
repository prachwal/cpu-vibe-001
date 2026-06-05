namespace Mos6502.Instructions.Lda;

/// <summary>
/// LDA zp — Load Accumulator from Zero Page
/// Opcode: $A5 | Size: 2 bytes | Cycles: 3
/// A = [zp]
/// </summary>
public static class LdaZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
        cpu.Cycles += 3;
    }
}
