namespace CpuVibe.Instructions.Cpy;

/// <summary>
/// CPY abs — Compare Y Register (Absolute)
/// Opcode: $CC | Size: 3 bytes | Cycles: 4
/// Flags: N, Z, C
/// </summary>
public static class CpyAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.Y - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.Y >= operand);
        cpu.Cycles += 4;
    }
}
