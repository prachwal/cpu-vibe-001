namespace Mos6502.Instructions.Cpy;

/// <summary>
/// CPY zp — Compare Y Register (Zero Page)
/// Opcode: $C4 | Size: 2 bytes | Cycles: 3
/// Flags: N, Z, C
/// </summary>
public static class CpyZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte operand = cpu.Memory.Read(addr);
        ushort result = (ushort)(cpu.Regs.Y - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.Y >= operand);
        cpu.Cycles += 3;
    }
}
