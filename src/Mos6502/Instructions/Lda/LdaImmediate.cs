namespace Mos6502.Instructions.Lda;

/// <summary>
/// LDA #imm — Load Accumulator with Immediate Value
/// Opcode: $A9 | Size: 2 bytes | Cycles: 2
/// A = imm
/// </summary>
public static class LdaImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte value = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
        cpu.Cycles += 2;
    }
}
