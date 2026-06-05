namespace Mos6502.Instructions.Cmp;

/// <summary>
/// CMP (zp),Y — Compare Accumulator (Indirect Indexed)
/// Opcode: $D1 | Size: 2 bytes | Cycles: 5 (+1 page crossing)
/// Flags: N, Z, C
/// </summary>
public static class CmpIndirectY
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
        ushort result = (ushort)(cpu.Regs.A - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.A >= operand);
        cpu.Cycles += 5;
    }
}
