namespace CpuVibe.Instructions.Cpx;

/// <summary>
/// CPX abs — Compare X Register (Absolute)
/// Opcode: $EC | Size: 3 bytes | Cycles: 4
/// Flags: N, Z, C
/// </summary>
public static class CpxAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.X - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.X >= operand);
        cpu.Cycles += 4;
    }
}
