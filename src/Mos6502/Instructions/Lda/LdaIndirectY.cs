namespace Mos6502.Instructions.Lda;

/// <summary>
/// LDA (zp),Y — Load Accumulator from Indirect Indexed
/// Opcode: $B1 | Size: 2 bytes | Cycles: 5 (+1 if page crossing)
/// A = [ptr] + Y
/// </summary>
public static class LdaIndirectY
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte lo = cpu.Memory.Read(zp);
        byte hi = cpu.Memory.Read((byte)((zp + 1) & 0xFF));
        ushort baseAddr = (ushort)((hi << 8) | lo);
        ushort finalAddr = (ushort)(baseAddr + cpu.Regs.Y);
        cpu.Cycles += 5;
        cpu.Cycles += (byte)((baseAddr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        byte value = cpu.Memory.Read(finalAddr);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
    }
}
