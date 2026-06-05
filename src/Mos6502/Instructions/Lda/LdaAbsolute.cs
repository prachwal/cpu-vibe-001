namespace Mos6502.Instructions.Lda;

/// <summary>
/// LDA abs — Load Accumulator from Absolute Address
/// Opcode: $AD | Size: 3 bytes | Cycles: 4
/// A = [addr]
/// </summary>
public static class LdaAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
        cpu.Cycles += 4;
    }
}
