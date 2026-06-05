namespace Mos6502.Instructions.Cmp;

/// <summary>
/// CMP zp,X — Compare Accumulator (Zero Page,X)
/// Opcode: $D5 | Size: 2 bytes | Cycles: 4
/// Flags: N, Z, C
/// </summary>
public static class CmpZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.A - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.A >= operand);
        cpu.Cycles += 4;
    }
}
