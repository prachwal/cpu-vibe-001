namespace CpuVibe.Instructions.Cmp;

/// <summary>
/// CMP zp — Compare Accumulator (Zero Page)
/// Opcode: $C5 | Size: 2 bytes | Cycles: 3
/// Flags: N, Z, C
/// </summary>
public static class CmpZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.A - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.A >= operand);
        cpu.Cycles += 3;
    }
}
