namespace CpuVibe.Instructions.Cpy;

/// <summary>
/// CPY #imm — Compare Y Register (Immediate)
/// Opcode: $C0 | Size: 2 bytes | Cycles: 2
/// Compare: Y - imm
/// Flags: N, Z, C
/// </summary>
public static class CpyImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte operand = cpu.Memory.Read(cpu.Regs.PC++);
        ushort result = (ushort)(cpu.Regs.Y - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.Y >= operand);
        cpu.Cycles += 2;
    }
}
