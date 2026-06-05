namespace Mos6502.Instructions.Eor;

/// <summary>
/// EOR (zp),Y — Bitwise XOR (Indirect Indexed)
/// Opcode: $51 | Size: 2 bytes | Cycles: 5 (+1 page crossing)
/// A = A ^ [ptr] + Y
/// Flags: N, Z
/// </summary>
public static class EorIndirectY
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte lo = cpu.Memory.Read(zp);
        byte hi = cpu.Memory.Read((byte)((zp + 1) & 0xFF));
        ushort baseAddr = (ushort)((hi << 8) | lo);
        ushort finalAddr = (ushort)(baseAddr + cpu.Regs.Y);
        cpu.Cycles += (byte)((baseAddr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        byte operand = cpu.Memory.Read(finalAddr);
        byte result = (byte)(cpu.Regs.A ^ operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 5;
    }
}
