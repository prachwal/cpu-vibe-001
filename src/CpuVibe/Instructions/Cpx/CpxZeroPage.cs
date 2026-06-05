namespace CpuVibe.Instructions.Cpx;

/// <summary>
/// CPX zp — Compare X Register (Zero Page)
/// Opcode: $E4 | Size: 2 bytes | Cycles: 3
/// Flags: N, Z, C
/// </summary>
public static class CpxZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.X - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.X >= operand);
        cpu.Cycles += 3;
    }
}
