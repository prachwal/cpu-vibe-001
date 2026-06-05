namespace Mos6502.Instructions.Cmp;

/// <summary>
/// CMP (zp,X) — Compare Accumulator (Indexed Indirect)
/// Opcode: $C1 | Size: 2 bytes | Cycles: 6
/// Flags: N, Z, C
/// </summary>
public static class CmpIndirectX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte ptr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte lo = cpu.Memory.Read(ptr);
        byte hi = cpu.Memory.Read((byte)((ptr + 1) & 0xFF));
        ushort addr = (ushort)((hi << 8) | lo);
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.A - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.A >= operand);
        cpu.Cycles += 6;
    }
}
