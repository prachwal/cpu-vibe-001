namespace Mos6502.Instructions.Lda;

/// <summary>
/// LDA (zp,X) — Load Accumulator from Indexed Indirect
/// Opcode: $A1 | Size: 2 bytes | Cycles: 6
/// A = [ptr + X]
/// </summary>
public static class LdaIndirectX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte ptr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte lo = cpu.Memory.Read(ptr);
        byte hi = cpu.Memory.Read((byte)((ptr + 1) & 0xFF));
        ushort addr = (ushort)((hi << 8) | lo);
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
        cpu.Cycles += 6;
    }
}
