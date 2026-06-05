namespace CpuVibe.Instructions.Cmp;

/// <summary>
/// CMP abs — Compare Accumulator (Absolute)
/// Opcode: $CD | Size: 3 bytes | Cycles: 4
/// Flags: N, Z, C
/// </summary>
public static class CmpAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.A - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.A >= operand);
        cpu.Cycles += 4;
    }
}
